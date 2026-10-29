<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="handleSearch">
          <el-form-item>
            <el-input v-model="query.keyword" placeholder="Inbound Order No." clearable style="width: 190px" />
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.status" placeholder="Status" clearable style="width: 130px">
              <el-option v-for="item in statusOptions" :key="item.value" :label="item.label" :value="item.value" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.warehouseId" placeholder="Inbound Warehouse" clearable style="width: 150px">
              <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-date-picker
              v-model="dateRange"
              type="daterange"
              value-format="yyyy-MM-dd"
              range-separator="to"
              start-placeholder="Start date"
              end-placeholder="End date"
              style="width: 260px"
            />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" icon="el-icon-search" @click="handleSearch">Search</el-button>
            <el-button icon="el-icon-refresh" @click="handleReset">Reset</el-button>
          </el-form-item>
        </el-form>
        <el-button v-permission="'inbound:edit'" type="primary" icon="el-icon-plus" @click="openCreate">New Inbound Order</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="orderNo" label="Inbound Order No." width="180" />
        <el-table-column prop="type" label="Inbound Type" width="120">
          <template slot-scope="{ row }">{{ inboundTypeName(row.type) }}</template>
        </el-table-column>
        <el-table-column prop="supplierName" label="Supplier" min-width="150" show-overflow-tooltip />
        <el-table-column prop="warehouseName" label="Inbound Warehouse" width="160" />
        <el-table-column prop="plannedDate" label="Planned Date" width="120">
          <template slot-scope="{ row }">{{ formatDate(row.plannedDate) }}</template>
        </el-table-column>
        <el-table-column prop="itemCount" label="Items" width="80" align="center" />
        <el-table-column prop="totalQty" label="Planned Qty" width="100" align="right" />
        <el-table-column prop="status" label="Status" width="100" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="statusTagType(row.status)" size="mini">{{ statusName(row.status) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="createdByName" label="Created By" width="100" />
        <el-table-column label="Actions" width="280" fixed="right">
          <template slot-scope="{ row }">
            <el-button type="text" @click="openDetail(row)">View</el-button>
            <el-button v-if="row.status === 0" v-permission="'inbound:edit'" type="text" @click="openEdit(row)">Edit</el-button>
            <el-button v-if="row.status === 0" v-permission="'inbound:edit'" type="text" @click="handleConfirm(row)">Confirm</el-button>
            <el-button v-if="row.status === 1" v-permission="'inbound:execute'" type="text" @click="handleComplete(row)">Execute Inbound</el-button>
            <el-button v-if="row.status === 0" v-permission="'inbound:edit'" type="text" class="text-danger" @click="handleDelete(row)">Delete</el-button>
            <el-button
              v-if="row.status === 0 || row.status === 1"
              v-permission="'inbound:edit'"
              type="text"
              class="text-danger"
              @click="handleCancel(row)"
            >Cancel</el-button>
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

    <el-dialog :title="form.id ? 'Edit Inbound Order' : 'New Inbound Order'" :visible.sync="dialogVisible" width="900px">
      <el-form ref="form" :model="form" :rules="rules" label-width="90px">
        <el-row :gutter="16">
          <el-col :span="8">
            <el-form-item label="Inbound Type" prop="type">
              <el-select v-model="form.type" style="width: 100%">
                <el-option label="Purchase Inbound" :value="1" />
                <el-option label="Production Inbound" :value="2" />
                <el-option label="Return Inbound" :value="3" />
                <el-option label="Other Inbound" :value="9" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="Supplier">
              <el-select v-model="form.supplierId" clearable style="width: 100%">
                <el-option v-for="item in suppliers" :key="item.id" :label="item.name" :value="item.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="Inbound Warehouse" prop="warehouseId">
              <el-select v-model="form.warehouseId" style="width: 100%" @change="handleWarehouseChange">
                <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="Planned Date" prop="plannedDate">
              <el-date-picker v-model="form.plannedDate" type="date" value-format="yyyy-MM-dd" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="16">
            <el-form-item label="Remarks">
              <el-input v-model="form.remark" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>

      <div class="table-toolbar">
        <span class="card-title">Inbound Items</span>
        <el-button type="primary" plain icon="el-icon-plus" size="mini" @click="addItem">Add Item</el-button>
      </div>
      <el-table :data="form.items" border size="mini">
        <el-table-column label="Material" min-width="180">
          <template slot-scope="{ row }">
            <el-select v-model="row.materialId" filterable placeholder="Select material" style="width: 100%">
              <el-option v-for="item in materials" :key="item.id" :label="`${item.code} ${item.name}`" :value="item.id" />
            </el-select>
          </template>
        </el-table-column>
        <el-table-column label="Location" min-width="160">
          <template slot-scope="{ row }">
            <el-select v-model="row.locationId" filterable clearable placeholder="Select location" style="width: 100%">
              <el-option v-for="item in locations" :key="item.id" :label="`${item.code} ${item.name}`" :value="item.id" />
            </el-select>
          </template>
        </el-table-column>
        <el-table-column label="Batch No." width="130">
          <template slot-scope="{ row }">
            <el-input v-model="row.batchNo" placeholder="Optional" />
          </template>
        </el-table-column>
        <el-table-column label="Planned Qty" width="130">
          <template slot-scope="{ row }">
            <el-input-number v-model="row.plannedQty" :min="0" :precision="2" :controls="false" style="width: 100%" />
          </template>
        </el-table-column>
        <el-table-column label="Unit Price" width="110">
          <template slot-scope="{ row }">
            <el-input-number v-model="row.price" :min="0" :precision="2" :controls="false" style="width: 100%" />
          </template>
        </el-table-column>
        <el-table-column label="Remarks" min-width="140">
          <template slot-scope="{ row }">
            <el-input v-model="row.remark" />
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

    <el-drawer title="Inbound Order Details" :visible.sync="detailVisible" size="760px">
      <div v-if="detail" class="detail-body">
        <el-descriptions :column="2" border size="small">
          <el-descriptions-item label="Inbound Order No.">{{ detail.orderNo }}</el-descriptions-item>
          <el-descriptions-item label="Status">{{ statusName(detail.status) }}</el-descriptions-item>
          <el-descriptions-item label="Inbound Type">{{ inboundTypeName(detail.type) }}</el-descriptions-item>
          <el-descriptions-item label="Supplier">{{ detail.supplierName || '—' }}</el-descriptions-item>
          <el-descriptions-item label="Inbound Warehouse">{{ detail.warehouseName }}</el-descriptions-item>
          <el-descriptions-item label="Planned Date">{{ formatDate(detail.plannedDate) }}</el-descriptions-item>
          <el-descriptions-item label="Created By">{{ detail.createdByName || '—' }}</el-descriptions-item>
          <el-descriptions-item label="Created At">{{ formatDateTime(detail.createdAt) }}</el-descriptions-item>
          <el-descriptions-item label="Remarks" :span="2">{{ detail.remark || '—' }}</el-descriptions-item>
        </el-descriptions>

        <el-table :data="detail.items" border size="mini" style="margin-top: 16px">
          <el-table-column prop="materialCode" label="Material Code" width="120" />
          <el-table-column prop="materialName" label="Material Name" min-width="150" show-overflow-tooltip />
          <el-table-column prop="locationCode" label="Location" width="110" />
          <el-table-column prop="batchNo" label="Batch No." width="120" />
          <el-table-column prop="plannedQty" label="Planned Qty" width="100" align="right" />
          <el-table-column prop="receivedQty" label="Received Qty" width="100" align="right" />
          <el-table-column prop="price" label="Unit Price" width="90" align="right" />
        </el-table>
      </div>
    </el-drawer>
  </div>
</template>

<script>
import { inboundApi } from '@/api/orders'
import { warehouseApi, locationApi, supplierApi, materialApi } from '@/api/masterdata'

const emptyForm = () => ({
  id: null,
  type: 1,
  supplierId: undefined,
  warehouseId: undefined,
  plannedDate: new Date().toISOString().substring(0, 10),
  remark: '',
  items: []
})

export default {
  name: 'Inbound',
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
      suppliers: [],
      materials: [],
      locations: [],
      dateRange: [],
      query: { page: 1, pageSize: 20, keyword: '', status: undefined, warehouseId: undefined },
      form: emptyForm(),
      statusOptions: [
        { label: 'Draft', value: 0 },
        { label: 'Confirmed', value: 1 },
        { label: 'Completed', value: 2 },
        { label: 'Cancelled', value: 3 }
      ],
      rules: {
        type: [{ required: true, message: 'Select inbound type', trigger: 'change' }],
        warehouseId: [{ required: true, message: 'Select inbound warehouse', trigger: 'change' }],
        plannedDate: [{ required: true, message: 'Select planned date', trigger: 'change' }]
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
    inboundTypeName(type) {
      return { 1: 'Purchase Inbound', 2: 'Production Inbound', 3: 'Return Inbound', 9: 'Other Inbound' }[type] || 'Other'
    },
    formatDate(value) {
      return value ? value.substring(0, 10) : '—'
    },
    formatDateTime(value) {
      return value ? value.replace('T', ' ').substring(0, 19) : '—'
    },
    async loadOptions() {
      const [warehouses, suppliers, materials] = await Promise.all([
        warehouseApi.options(),
        supplierApi.options(),
        materialApi.options()
      ])
      this.warehouses = warehouses
      this.suppliers = suppliers
      this.materials = materials
    },
    async loadLocations(warehouseId) {
      this.locations = warehouseId ? await locationApi.options({ warehouseId }) : []
    },
    async loadData() {
      this.loading = true
      try {
        const params = { ...this.query }
        if (this.dateRange && this.dateRange.length === 2) {
          params.startDate = this.dateRange[0]
          params.endDate = this.dateRange[1]
        }
        const result = await inboundApi.page(params)
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
      this.dateRange = []
      this.loadData()
    },
    handleWarehouseChange(warehouseId) {
      this.form.items.forEach(item => {
        item.locationId = undefined
      })
      this.loadLocations(warehouseId)
    },
    addItem() {
      this.form.items.push({ materialId: undefined, locationId: undefined, batchNo: '', plannedQty: 1, price: 0, remark: '' })
    },
    openCreate() {
      this.form = emptyForm()
      this.locations = []
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    async openEdit(row) {
      const detail = await inboundApi.get(row.id)
      this.form = {
        id: detail.id,
        type: detail.type,
        supplierId: detail.supplierId,
        warehouseId: detail.warehouseId,
        plannedDate: detail.plannedDate.substring(0, 10),
        remark: detail.remark,
        items: detail.items.map(item => ({
          materialId: item.materialId,
          locationId: item.locationId,
          batchNo: item.batchNo,
          plannedQty: item.plannedQty,
          price: item.price,
          remark: item.remark
        }))
      }
      await this.loadLocations(detail.warehouseId)
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    async openDetail(row) {
      this.detail = await inboundApi.get(row.id)
      this.detailVisible = true
    },
    submit() {
      this.$refs.form.validate(async valid => {
        if (!valid) return
        if (!this.form.items.length) {
          this.$message.warning('Add at least one inbound item')
          return
        }
        const invalid = this.form.items.some(item => !item.materialId || item.plannedQty <= 0)
        if (invalid) {
          this.$message.warning('Please complete material and planned qty for all items')
          return
        }
        this.saving = true
        try {
          if (this.form.id) {
            await inboundApi.update(this.form.id, this.form)
          } else {
            await inboundApi.create(this.form)
          }
          this.$message.success('Saved successfully')
          this.dialogVisible = false
          this.loadData()
        } finally {
          this.saving = false
        }
      })
    },
    handleConfirm(row) {
      this.$confirm(`Confirm inbound order "${row.orderNo}"?`, 'Confirm', { type: 'warning' })
        .then(async () => {
          await inboundApi.confirm(row.id)
          this.$message.success('Inbound order confirmed')
          this.loadData()
        })
        .catch(() => {})
    },
    handleComplete(row) {
      this.$confirm(`Executing inbound will increase stock. Continue?`, 'Confirm', { type: 'warning' })
        .then(async () => {
          await inboundApi.complete(row.id)
          this.$message.success('Inbound completed, stock updated')
          this.loadData()
        })
        .catch(() => {})
    },
    handleCancel(row) {
      this.$confirm(`Cancel inbound order "${row.orderNo}"?`, 'Confirm', { type: 'warning' })
        .then(async () => {
          await inboundApi.cancel(row.id)
          this.$message.success('Inbound order cancelled')
          this.loadData()
        })
        .catch(() => {})
    },
    handleDelete(row) {
      this.$confirm(`Delete inbound order "${row.orderNo}"?`, 'Confirm', { type: 'warning' })
        .then(async () => {
          await inboundApi.remove(row.id)
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
