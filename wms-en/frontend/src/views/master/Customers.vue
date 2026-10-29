<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="handleSearch">
          <el-form-item>
            <el-input v-model="query.keyword" placeholder="Code / Name / Contact" clearable style="width: 220px" />
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.status" placeholder="Status" clearable style="width: 120px">
              <el-option label="Enabled" :value="1" />
              <el-option label="Disabled" :value="0" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" icon="el-icon-search" @click="handleSearch">Search</el-button>
            <el-button icon="el-icon-refresh" @click="handleReset">Reset</el-button>
          </el-form-item>
        </el-form>
        <el-button v-permission="'customer:edit'" type="primary" icon="el-icon-plus" @click="openCreate">New Customer</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="code" label="Customer Code" width="150" />
        <el-table-column prop="name" label="Customer Name" min-width="180" show-overflow-tooltip />
        <el-table-column prop="contact" label="Contact" width="120" />
        <el-table-column prop="phone" label="Phone" width="140" />
        <el-table-column prop="address" label="Address" min-width="200" show-overflow-tooltip />
        <el-table-column prop="status" label="Status" width="90" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'info'" size="mini">
              {{ row.status === 1 ? 'Enabled' : 'Disabled' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="Actions" width="150" fixed="right">
          <template slot-scope="{ row }">
            <el-button v-permission="'customer:edit'" type="text" @click="openEdit(row)">Edit</el-button>
            <el-button v-permission="'customer:edit'" type="text" class="text-danger" @click="handleDelete(row)">Delete</el-button>
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

    <el-dialog :title="form.id ? 'Edit Customer' : 'New Customer'" :visible.sync="dialogVisible" width="560px">
      <el-form ref="form" :model="form" :rules="rules" label-width="90px">
        <el-form-item label="Customer Code" prop="code">
          <el-input v-model="form.code" />
        </el-form-item>
        <el-form-item label="Customer Name" prop="name">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="Contact">
          <el-input v-model="form.contact" />
        </el-form-item>
        <el-form-item label="Phone">
          <el-input v-model="form.phone" />
        </el-form-item>
        <el-form-item label="Address">
          <el-input v-model="form.address" />
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
import { customerApi } from '@/api/masterdata'

const emptyForm = () => ({
  id: null,
  code: '',
  name: '',
  contact: '',
  phone: '',
  address: '',
  status: 1,
  remark: ''
})

export default {
  name: 'Customers',
  data() {
    return {
      loading: false,
      saving: false,
      dialogVisible: false,
      list: [],
      total: 0,
      query: { page: 1, pageSize: 20, keyword: '', status: undefined },
      form: emptyForm(),
      rules: {
        code: [{ required: true, message: 'Enter customer code', trigger: 'blur' }],
        name: [{ required: true, message: 'Enter customer name', trigger: 'blur' }]
      }
    }
  },
  created() {
    this.loadData()
  },
  methods: {
    async loadData() {
      this.loading = true
      try {
        const result = await customerApi.page(this.query)
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
            await customerApi.update(this.form.id, this.form)
          } else {
            await customerApi.create(this.form)
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
      this.$confirm(`Delete customer "${row.name}"?`, 'Confirm', { type: 'warning' })
        .then(async () => {
          await customerApi.remove(row.id)
          this.$message.success('Deleted successfully')
          this.loadData()
        })
        .catch(() => {})
    }
  }
}
</script>
