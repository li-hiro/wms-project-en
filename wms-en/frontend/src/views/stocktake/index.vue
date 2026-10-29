<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="handleSearch">
          <el-form-item>
            <el-select v-model="query.status" placeholder="Status" clearable style="width: 140px">
              <el-option label="In Progress" :value="0" />
              <el-option label="Completed" :value="1" />
              <el-option label="Cancelled" :value="2" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.warehouseId" placeholder="Stocktake Warehouse" clearable style="width: 160px">
              <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" icon="el-icon-search" @click="handleSearch">Search</el-button>
            <el-button icon="el-icon-refresh" @click="handleReset">Reset</el-button>
          </el-form-item>
        </el-form>
        <el-button v-permission="'stocktake:edit'" type="primary" icon="el-icon-plus" @click="openCreate">New Stocktake Order</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="orderNo" label="Stocktake Order No." width="180" />
        <el-table-column prop="warehouseName" label="Stocktake Warehouse" width="170" />
        <el-table-column prop="takeDate" label="Stocktake Date" width="120">
          <template slot-scope="{ row }">{{ formatDate(row.takeDate) }}</template>
        </el-table-column>
        <el-table-column prop="itemCount" label="Items" width="90" align="center" />
        <el-table-column prop="diffCount" label="Variances" width="90" align="center">
          <template slot-scope="{ row }">
            <span :class="{ 'text-danger': row.diffCount > 0 }">{{ row.diffCount }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="Status" width="100" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="statusTagType(row.status)" size="mini">{{ statusName(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdByName" label="Created By" width="100" />
        <el-table-column prop="createdAt" label="Created At" width="160">
          <template slot-scope="{ row }">{{ formatDateTime(row.createdAt) }}</template>
        </el-table-column>
        <el-table-column label="Actions" width="250" fixed="right">
          <template slot-scope="{ row }">
            <el-button type="text" @click="openDetail(row)">{{ row.status === 0 ? 'Enter Count' : 'View' }}</el-button>
            <el-button v-if="row.status === 0" v-permission="'stocktake:execute'" type="text" @click="handleComplete(row)">Complete Stocktake</el-button>
            <el-button v-if="row.status === 0" v-permission="'stocktake:edit'" type="text" class="text-danger" @click="handleCancel(row)">Cancel</el-button>
            <el-button v-if="row.status === 0" v-permission="'stocktake:edit'" type="text" class="text-danger" @click="handleDelete(row)">Delete</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-bar">
        <el-pagination
          background
          layout="total, sizes, prev, pager, next"
          :total="total"
          :current-page.sync="query.page"
          :page-size.sync="query.pageSize"
          :page-sizes="[10, 20, 50]"
          @current-change="loadData"
          @size-change="handleSearch"
        />
      </div>
    </div>

    <el-dialog title="New Stocktake Order" :visible.sync="createVisible" width="480px">
      <el-form ref="createForm" :model="createForm" :rules="createRules" label-width="90px">
        <el-form-item label="Stocktake Warehouse" prop="warehouseId">
          <el-select v-model="createForm.warehouseId" style="width: 100%">
            <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="Stocktake Date" prop="takeDate">
          <el-date-picker v-model="createForm.takeDate" type="date" value-format="yyyy-MM-dd" style="width: 100%" />
        </el-form-item>
        <el-form-item label="Remarks">
          <el-input v-model="createForm.remark" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <div class="form-tip">The system will generate stocktake items from the current stock of the selected warehouse.</div>
      <span slot="footer">
        <el-button @click="createVisible = false">Cancel</el-button>
        <el-button type="primary" :loading="saving" @click="submitCreate">Generate Order</el-button>
      </span>
    </el-dialog>

    <el-drawer :title="`Stocktake Order ${detail ? detail.orderNo : ''}`" :visible.sync="detailVisible" size="900px">
      <div v-if="detail" class="detail-body">
        <el-descriptions :column="3" border size="small">
          <el-descriptions-item label="Stocktake Warehouse">{{ detail.warehouseName }}</el-descriptions-item>
          <el-descriptions-item label="Stocktake Date">{{ formatDate(detail.takeDate) }}</el-descriptions-item>
          <el-descriptions-item label="Status">{{ statusName(detail.status) }}</el-descriptions-item>
          <el-descriptions-item label="Remarks" :span="3">{{ detail.remark || '—' }}</el-descriptions-item>
        </el-descriptions>

        <el-table :data="detail.items" border size="mini" style="margin-top: 16px">
          <el-table-column prop="materialCode" label="Material Code" width="120" />
          <el-table-column prop="materialName" label="Material Name" min-width="150" show-overflow-tooltip />
          <el-table-column prop="locationCode" label="Location" width="110" />
          <el-table-column prop="batchNo" label="Batch No." width="120" />
          <el-table-column prop="systemQty" label="Book Qty" width="100" align="right" />
          <el-table-column label="Actual Qty" width="130">
            <template slot-scope="{ row }">
              <el-input-number
                v-if="detail.status === 0"
                v-model="row.countedQty"
                :min="0"
                :precision="2"
                :controls="false"
                size="mini"
                style="width: 100%"
                @change="row.diffQty = row.countedQty - row.systemQty"
              />
              <span v-else>{{ row.countedQty }}</span>
            </template>
          </el-table-column>
          <el-table-column label="Variance" width="100" align="right">
            <template slot-scope="{ row }">
              <span :class="row.countedQty - row.systemQty !== 0 ? 'text-danger' : 'text-muted'">
                {{ row.countedQty - row.systemQty }}
              </span>
            </template>
          </el-table-column>
        </el-table>

        <div v-if="detail.status === 0" class="drawer-footer">
          <el-button @click="detailVisible = false">Cancel</el-button>
          <el-button type="primary" :loading="saving" @click="saveCounted">Save Actual Qty</el-button>
        </div>
      </div>
    </el-drawer>
  </div>
</template>

<script>
import { stockTakeApi } from '@/api/orders'
import { warehouseApi } from '@/api/masterdata'

export default {
  name: 'StockTake',
  data() {
    return {
      loading: false,
      saving: false,
      createVisible: false,
      detailVisible: false,
      detail: null,
      list: [],
      total: 0,
      warehouses: [],
      query: { page: 1, pageSize: 20, keyword: '', status: undefined, warehouseId: undefined },
      createForm: {
        warehouseId: undefined,
        takeDate: new Date().toISOString().substring(0, 10),
        remark: ''
      },
      createRules: {
        warehouseId: [{ required: true, message: 'Select stocktake warehouse', trigger: 'change' }],
        takeDate: [{ required: true, message: 'Select stocktake date', trigger: 'change' }]
      }
    }
  },
  created() {
    this.loadWarehouses()
    this.loadData()
  },
  methods: {
    statusName(status) {
      return { 0: 'In Progress', 1: 'Completed', 2: 'Cancelled' }[status] || 'Unknown'
    },
    statusTagType(status) {
      return { 0: 'warning', 1: 'success', 2: 'info' }[status] || 'info'
    },
    formatDate(value) {
      return value ? value.substring(0, 10) : '—'
    },
    formatDateTime(value) {
      return value ? value.replace('T', ' ').substring(0, 19) : '—'
    },
    async loadWarehouses() {
      this.warehouses = await warehouseApi.options()
    },
    async loadData() {
      this.loading = true
      try {
        const result = await stockTakeApi.page(this.query)
        this.list = result.items
        this.total = result.total
      } finally {
        this.loading = false
      }
    },
    handleSearch() {
      this.query.page = 1
      this.loadData()
    },
    handleReset() {
      this.query = { page: 1, pageSize: 20, keyword: '', status: undefined, warehouseId: undefined }
      this.loadData()
    },
    openCreate() {
      this.createForm = {
        warehouseId: undefined,
        takeDate: new Date().toISOString().substring(0, 10),
        remark: ''
      }
      this.createVisible = true
      this.$nextTick(() => this.$refs.createForm && this.$refs.createForm.clearValidate())
    },
    submitCreate() {
      this.$refs.createForm.validate(async valid => {
        if (!valid) return
        this.saving = true
        try {
          await stockTakeApi.create(this.createForm)
          this.$message.success('Stocktake order created')
          this.createVisible = false
          this.loadData()
        } finally {
          this.saving = false
        }
      })
    },
    async openDetail(row) {
      this.detail = await stockTakeApi.get(row.id)
      this.detailVisible = true
    },
    saveCounted() {
      const items = this.detail.items.map(item => ({ id: item.id, countedQty: item.countedQty }))
      this.saving = true
      stockTakeApi
        .saveItems(this.detail.id, { items })
        .then(() => {
          this.$message.success('Actual quantities saved')
          this.detailVisible = false
          this.loadData()
        })
        .finally(() => {
          this.saving = false
        })
    },
    handleComplete(row) {
      this.$confirm('Completing the stocktake will automatically adjust stock based on variances. Continue?', 'Confirm', { type: 'warning' })
        .then(async () => {
          await stockTakeApi.complete(row.id)
          this.$message.success('Stocktake completed, stock adjusted')
          this.loadData()
        })
        .catch(() => {})
    },
    handleCancel(row) {
      this.$confirm(`Cancel stocktake order "${row.orderNo}"?`, 'Confirm', { type: 'warning' })
        .then(async () => {
          await stockTakeApi.cancel(row.id)
          this.$message.success('Stocktake order cancelled')
          this.loadData()
        })
        .catch(() => {})
    },
    handleDelete(row) {
      this.$confirm(`Delete stocktake order "${row.orderNo}"?`, 'Confirm', { type: 'warning' })
        .then(async () => {
          await stockTakeApi.remove(row.id)
          this.$message.success('Deleted successfully')
          this.loadData()
        })
        .catch(() => {})
    }
  }
}
</script>

<style scoped>
.detail-body {
  padding: 0 20px 20px;
}
.drawer-footer {
  margin-top: 16px;
  text-align: right;
}
</style>
