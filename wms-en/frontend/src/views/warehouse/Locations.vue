<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="handleSearch">
          <el-form-item>
            <el-select v-model="query.warehouseId" placeholder="Warehouse" clearable style="width: 180px">
              <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-input v-model="query.keyword" placeholder="Location Code / Name" clearable style="width: 200px" />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" icon="el-icon-search" @click="handleSearch">Search</el-button>
            <el-button icon="el-icon-refresh" @click="handleReset">Reset</el-button>
          </el-form-item>
        </el-form>
        <el-button v-permission="'location:edit'" type="primary" icon="el-icon-plus" @click="openCreate">New Location</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="warehouseName" label="Warehouse" width="140" />
        <el-table-column prop="code" label="Location Code" width="140" />
        <el-table-column prop="name" label="Location Name" min-width="140" show-overflow-tooltip />
        <el-table-column prop="zone" label="Zone" width="110" />
        <el-table-column prop="type" label="Type" width="100" align="center">
          <template slot-scope="{ row }">{{ typeName(row.type) }}</template>
        </el-table-column>
        <el-table-column prop="capacity" label="Capacity" width="100" align="right" />
        <el-table-column prop="onHandQty" label="On-hand Qty" width="120" align="right" />
        <el-table-column prop="status" label="Status" width="90" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'info'" size="mini">
              {{ row.status === 1 ? 'Enabled' : 'Disabled' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="Actions" width="150" fixed="right">
          <template slot-scope="{ row }">
            <el-button v-permission="'location:edit'" type="text" @click="openEdit(row)">Edit</el-button>
            <el-button v-permission="'location:edit'" type="text" class="text-danger" @click="handleDelete(row)">Delete</el-button>
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

    <el-dialog :title="form.id ? 'Edit Location' : 'New Location'" :visible.sync="dialogVisible" width="560px">
      <el-form ref="form" :model="form" :rules="rules" label-width="90px">
        <el-form-item label="Warehouse" prop="warehouseId">
          <el-select v-model="form.warehouseId" placeholder="Select warehouse" style="width: 100%">
            <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="Location Code" prop="code">
          <el-input v-model="form.code" placeholder="e.g. A-01-01" />
        </el-form-item>
        <el-form-item label="Location Name" prop="name">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="Zone">
          <el-input v-model="form.zone" placeholder="e.g. Zone A" />
        </el-form-item>
        <el-form-item label="Location Type">
          <el-select v-model="form.type" style="width: 100%">
            <el-option label="Storage" :value="1" />
            <el-option label="Receiving" :value="2" />
            <el-option label="Shipping" :value="3" />
            <el-option label="Other" :value="9" />
          </el-select>
        </el-form-item>
        <el-form-item label="Capacity">
          <el-input-number v-model="form.capacity" :min="0" :precision="2" style="width: 100%" />
        </el-form-item>
        <el-form-item label="Status">
          <el-switch v-model="form.status" :active-value="1" :inactive-value="0" active-text="Enabled" inactive-text="Disabled" />
        </el-form-item>
        <el-form-item label="Remarks">
          <el-input v-model="form.remark" type="textarea" :rows="2" />
        </el-form-item>
      </el-form>
      <span slot="footer">
        <el-button @click="dialogVisible = false">Cancel</el-button>
        <el-button type="primary" :loading="saving" @click="submit">Save</el-button>
      </span>
    </el-dialog>
  </div>
</template>

<script>
import { locationApi, warehouseApi } from '@/api/masterdata'

const emptyForm = () => ({
  id: null,
  warehouseId: undefined,
  code: '',
  name: '',
  zone: '',
  type: 1,
  capacity: 0,
  status: 1,
  remark: ''
})

export default {
  name: 'Locations',
  data() {
    return {
      loading: false,
      saving: false,
      dialogVisible: false,
      list: [],
      total: 0,
      warehouses: [],
      query: { page: 1, pageSize: 20, keyword: '', warehouseId: undefined },
      form: emptyForm(),
      rules: {
        warehouseId: [{ required: true, message: 'Select warehouse', trigger: 'change' }],
        code: [{ required: true, message: 'Enter location code', trigger: 'blur' }],
        name: [{ required: true, message: 'Enter location name', trigger: 'blur' }]
      }
    }
  },
  created() {
    this.loadWarehouses()
    this.loadData()
  },
  methods: {
    typeName(type) {
      return { 1: 'Storage', 2: 'Receiving', 3: 'Shipping', 9: 'Other' }[type] || 'Other'
    },
    async loadWarehouses() {
      this.warehouses = await warehouseApi.options()
    },
    async loadData() {
      this.loading = true
      try {
        const result = await locationApi.page(this.query)
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
      this.query = { page: 1, pageSize: 20, keyword: '', warehouseId: undefined }
      this.loadData()
    },
    openCreate() {
      this.form = emptyForm()
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    openEdit(row) {
      this.form = { ...row }
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    submit() {
      this.$refs.form.validate(async valid => {
        if (!valid) return
        this.saving = true
        try {
          if (this.form.id) {
            await locationApi.update(this.form.id, this.form)
          } else {
            await locationApi.create(this.form)
          }
          this.$message.success('Saved successfully')
          this.dialogVisible = false
          this.loadData()
        } finally {
          this.saving = false
        }
      })
    },
    handleDelete(row) {
      this.$confirm(`Delete location "${row.code}"?`, 'Confirm', { type: 'warning' })
        .then(async () => {
          await locationApi.remove(row.id)
          this.$message.success('Deleted successfully')
          this.loadData()
        })
        .catch(() => {})
    }
  }
}
</script>
