<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="handleSearch">
          <el-form-item>
            <el-input v-model="query.keyword" placeholder="Transfer Order No." clearable style="width: 190px" />
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.status" placeholder="Status" clearable style="width: 130px">
              <el-option label="Draft" :value="0" />
              <el-option label="Completed" :value="2" />
              <el-option label="Cancelled" :value="3" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" icon="el-icon-search" @click="handleSearch">Search</el-button>
            <el-button icon="el-icon-refresh" @click="handleReset">Reset</el-button>
          </el-form-item>
        </el-form>
        <el-button v-permission="'transfer:edit'" type="primary" icon="el-icon-plus" @click="openCreate">New Transfer Order</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="orderNo" label="Transfer Order No." width="180" />
        <el-table-column prop="fromWarehouseName" label="Source Warehouse" width="170" />
        <el-table-column prop="toWarehouseName" label="Destination Warehouse" width="170" />
        <el-table-column prop="transferDate" label="Transfer Date" width="120">
          <template slot-scope="{ row }">{{ formatDate(row.transferDate) }}</template>
        </el-table-column>
        <el-table-column prop="itemCount" label="Items" width="90" align="center" />
        <el-table-column prop="totalQty" label="Transfer Qty" width="100" align="right" />
        <el-table-column prop="status" label="Status" width="100" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="statusTagType(row.status)" size="mini">{{ statusName(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdByName" label="Created By" width="100" />
        <el-table-column label="Actions" width="250" fixed="right">
          <template slot-scope="{ row }">
            <el-button type="text" @click="openDetail(row)">View</el-button>
            <el-button v-if="row.status === 0" v-permission="'transfer:edit'" type="text" @click="openEdit(row)">Edit</el-button>
            <el-button v-if="row.status === 0" v-permission="'transfer:execute'" type="text" @click="handleComplete(row)">Execute Transfer</el-button>
            <el-button v-if="row.status === 0" v-permission="'transfer:edit'" type="text" class="text-danger" @click="handleDelete(row)">Delete</el-button>
            <el-button v-if="row.status === 0" v-permission="'transfer:edit'" type="text" class="text-danger" @click="handleCancel(row)">Cancel</el-button>
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

    <el-dialog :title="form.id ? 'Edit Transfer Order' : 'New Transfer Order'" :visible.sync="dialogVisible" width="960px">
      <el-form ref="form" :model="form" :rules="rules" label-width="90px">
        <el-row :gutter="16">
          <el-col :span="8">
            <el-form-item label="Source Warehouse" prop="fromWarehouseId">
              <el-select v-model="form.fromWarehouseId" style="width: 100%" @change="loadFromLocations">
                <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="Destination Warehouse" prop="toWarehouseId">
              <el-select v-model="form.toWarehouseId" style="width: 100%" @change="loadToLocations">
                <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="Transfer Date" prop="transferDate">
              <el-date-picker v-model="form.transferDate" type="date" value-format="yyyy-MM-dd" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item label="Remarks">
              <el-input v-model="form.remark" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>

      <div class="table-toolbar">
        <span class="card-title">Transfer Items</span>
        <el-button type="primary" plain icon="el-icon-plus" size="mini" @click="addItem">Add Item</el-button>
      </div>
      <el-table :data="form.items" border size="mini">
        <el-table-column label="Material" min-width="170">
          <template slot-scope="{ row }">
            <el-select v-model="row.materialId" filterable placeholder="Select material" style="width: 100%">
              <el-option v-for="item in materials" :key="item.id" :label="`${item.code} ${item.name}`" :value="item.id" />
            </el-select>
          </template>
        </el-table-column>
        <el-table-column label="Source Location" min-width="150">
          <template slot-scope="{ row }">
            <el-select v-model="row.fromLocationId" filterable placeholder="Select location" style="width: 100%">
              <el-option v-for="item in fromLocations" :key="item.id" :label="`${item.code} ${item.name}`" :value="item.id" />
            </el-select>
          </template>
        </el-table-column>
        <el-table-column label="Destination Location" min-width="150">
          <template slot-scope="{ row }">
            <el-select v-model="row.toLocationId" filterable placeholder="Select location" style="width: 100%">
              <el-option v-for="item in toLocations" :key="item.id" :label="`${item.code} ${item.name}`" :value="item.id" />
            </el-select>
          </template>
        </el-table-column>
        <el-table-column label="Batch No." width="120">
          <template slot-scope="{ row }">
            <el-input v-model="row.batchNo" placeholder="Optional" />
          </template>
        </el-table-column>
        <el-table-column label="Transfer Qty" width="120">
          <template slot-scope="{ row }">
            <el-input-number v-model="row.quantity" :min="0" :precision="2" :controls="false" style="width: 100%" />
          </template>
        </el-table-column>
        <el-table-column label="Actions" width="70" align="center">
          <template slot-scope="{ $index }">
            <el-button type="text" class="text-danger" @click="form.items.splice($index, 1)">Delete</el-button>
          </template>
        </el-table-column>
      </el-table>

      <span slot="footer">
        <el-button @click="dialogVisible = false">Cancel</el-button>
        <el-button type="primary" :loading="saving" @click="submit">Save</el-button>
      </span>
    </el-dialog>

    <el-drawer title="Transfer Order Details" :visible.sync="detailVisible" size="820px">
      <div v-if="detail" class="detail-body">
        <el-descriptions :column="3" border size="small">
          <el-descriptions-item label="Transfer Order No.">{{ detail.orderNo }}</el-descriptions-item>
          <el-descriptions-item label="Status">{{ statusName(detail.status) }}</el-descriptions-item>
          <el-descriptions-item label="Transfer Date">{{ formatDate(detail.transferDate) }}</el-descriptions-item>
          <el-descriptions-item label="Source Warehouse">{{ detail.fromWarehouseName }}</el-descriptions-item>
          <el-descriptions-item label="Destination Warehouse">{{ detail.toWarehouseName }}</el-descriptions-item>
          <el-descriptions-item label="Created By">{{ detail.createdByName || '—' }}</el-descriptions-item>
          <el-descriptions-item label="Remarks" :span="3">{{ detail.remark || '—' }}</el-descriptions-item>
        </el-descriptions>

        <el-table :data="detail.items" border size="mini" style="margin-top: 16px">
          <el-table-column prop="materialCode" label="Material Code" width="120" />
          <el-table-column prop="materialName" label="Material Name" min-width="150" show-overflow-tooltip />
          <el-table-column prop="fromLocationCode" label="Source Location" width="110" />
          <el-table-column prop="toLocationCode" label="Destination Location" width="110" />
          <el-table-column prop="batchNo" label="Batch No." width="120" />
          <el-table-column prop="quantity" label="Qty" width="100" align="right" />
        </el-table>
      </div>
    </el-drawer>
  </div>
</template>

<script>
import { transferApi } from '@/api/orders'
import { warehouseApi, locationApi, materialApi } from '@/api/masterdata'

const emptyForm = () => ({
  id: null,
  fromWarehouseId: undefined,
  toWarehouseId: undefined,
  transferDate: new Date().toISOString().substring(0, 10),
  remark: '',
  items: []
})

export default {
  name: 'Transfer',
  data() {
    return {
      loading: false,
      saving: false,
      dialogVisible: false,
      detailVisible: false,
      detail: null,
      list: [],
      total: 0,
      warehouses: [],
      materials: [],
      fromLocations: [],
      toLocations: [],
      query: { page: 1, pageSize: 20, keyword: '', status: undefined },
      form: emptyForm(),
      rules: {
        fromWarehouseId: [{ required: true, message: 'Select source warehouse', trigger: 'change' }],
        toWarehouseId: [{ required: true, message: 'Select destination warehouse', trigger: 'change' }],
        transferDate: [{ required: true, message: 'Select transfer date', trigger: 'change' }]
      }
    }
  },
  created() {
    this.loadOptions()
    this.loadData()
  },
  methods: {
    statusName(status) {
      return { 0: 'Draft', 1: 'Confirmed', 2: 'Completed', 3: 'Cancelled' }[status] || 'Unknown'
    },
    statusTagType(status) {
      return { 0: 'info', 1: 'warning', 2: 'success', 3: 'danger' }[status] || 'info'
    },
    formatDate(value) {
      return value ? value.substring(0, 10) : '—'
    },
    async loadOptions() {
      const [warehouses, materials] = await Promise.all([warehouseApi.options(), materialApi.options()])
      this.warehouses = warehouses
      this.materials = materials
    },
    async loadFromLocations(warehouseId) {
      this.fromLocations = warehouseId ? await locationApi.options({ warehouseId }) : []
      this.form.items.forEach(item => {
        item.fromLocationId = undefined
      })
    },
    async loadToLocations(warehouseId) {
      this.toLocations = warehouseId ? await locationApi.options({ warehouseId }) : []
      this.form.items.forEach(item => {
        item.toLocationId = undefined
      })
    },
    async loadData() {
      this.loading = true
      try {
        const result = await transferApi.page(this.query)
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
      this.query = { page: 1, pageSize: 20, keyword: '', status: undefined }
      this.loadData()
    },
    addItem() {
      this.form.items.push({
        materialId: undefined,
        fromLocationId: undefined,
        toLocationId: undefined,
        batchNo: '',
        quantity: 1
      })
    },
    openCreate() {
      this.form = emptyForm()
      this.fromLocations = []
      this.toLocations = []
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    async openEdit(row) {
      const detail = await transferApi.get(row.id)
      this.form = {
        id: detail.id,
        fromWarehouseId: detail.fromWarehouseId,
        toWarehouseId: detail.toWarehouseId,
        transferDate: detail.transferDate.substring(0, 10),
        remark: detail.remark,
        items: detail.items.map(item => ({
          materialId: item.materialId,
          fromLocationId: item.fromLocationId,
          toLocationId: item.toLocationId,
          batchNo: item.batchNo,
          quantity: item.quantity
        }))
      }
      this.fromLocations = await locationApi.options({ warehouseId: detail.fromWarehouseId })
      this.toLocations = await locationApi.options({ warehouseId: detail.toWarehouseId })
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    async openDetail(row) {
      this.detail = await transferApi.get(row.id)
      this.detailVisible = true
    },
    submit() {
      this.$refs.form.validate(async valid => {
        if (!valid) return
        if (this.form.fromWarehouseId === this.form.toWarehouseId) {
          this.$message.warning('Source and destination warehouses cannot be the same')
          return
        }
        if (!this.form.items.length) {
          this.$message.warning('Add at least one transfer item')
          return
        }
        const invalid = this.form.items.some(
          item => !item.materialId || !item.fromLocationId || !item.toLocationId || item.quantity <= 0
        )
        if (invalid) {
          this.$message.warning('Please complete material, location and quantity for all items')
          return
        }
        this.saving = true
        try {
          if (this.form.id) {
            await transferApi.update(this.form.id, this.form)
          } else {
            await transferApi.create(this.form)
          }
          this.$message.success('Saved successfully')
          this.dialogVisible = false
          this.loadData()
        } finally {
          this.saving = false
        }
      })
    },
    handleComplete(row) {
      this.$confirm('Executing the transfer will decrease stock at the source location and increase stock at the destination location. Continue?', 'Confirm', { type: 'warning' })
        .then(async () => {
          await transferApi.complete(row.id)
          this.$message.success('Transfer completed, stock moved')
          this.loadData()
        })
        .catch(() => {})
    },
    handleCancel(row) {
      this.$confirm(`Cancel transfer order "${row.orderNo}"?`, 'Confirm', { type: 'warning' })
        .then(async () => {
          await transferApi.cancel(row.id)
          this.$message.success('Transfer order cancelled')
          this.loadData()
        })
        .catch(() => {})
    },
    handleDelete(row) {
      this.$confirm(`Delete transfer order "${row.orderNo}"?`, 'Confirm', { type: 'warning' })
        .then(async () => {
          await transferApi.remove(row.id)
          this.$message.success('Deleted successfully')
          this.loadData()
        })
        .catch(() => {})
    }
  }
}
</script>

<style scoped>
.card-title {
  font-size: 14px;
  font-weight: 600;
}
.detail-body {
  padding: 0 20px 20px;
}
</style>
