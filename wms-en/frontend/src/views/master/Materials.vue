<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="handleSearch">
          <el-form-item>
            <el-input v-model="query.keyword" placeholder="Code / Name / Spec" clearable style="width: 220px" />
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.categoryId" placeholder="Material Category" clearable style="width: 160px">
              <el-option v-for="item in categories" :key="item.id" :label="item.name" :value="item.id" />
            </el-select>
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
        <el-button v-permission="'material:edit'" type="primary" icon="el-icon-plus" @click="openCreate">New Material</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="code" label="Material Code" width="140" />
        <el-table-column prop="name" label="Material Name" min-width="160" show-overflow-tooltip />
        <el-table-column prop="spec" label="Spec" min-width="140" show-overflow-tooltip />
        <el-table-column prop="categoryName" label="Category" width="120" />
        <el-table-column prop="unit" label="Unit" width="80" align="center" />
        <el-table-column prop="price" label="Unit Price" width="100" align="right" />
        <el-table-column prop="safetyStock" label="Safety Stock" width="120" align="right" />
        <el-table-column prop="onHandQty" label="On-hand Qty" width="120" align="right">
          <template slot-scope="{ row }">
            <span :class="{ 'text-danger': row.onHandQty < row.safetyStock }">{{ row.onHandQty }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="Status" width="90" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'info'" size="mini">
              {{ row.status === 1 ? 'Enabled' : 'Disabled' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="Actions" width="150" fixed="right">
          <template slot-scope="{ row }">
            <el-button v-permission="'material:edit'" type="text" @click="openEdit(row)">Edit</el-button>
            <el-button v-permission="'material:edit'" type="text" class="text-danger" @click="handleDelete(row)">Delete</el-button>
          </template>
        </el-table-column>
      </el-table>

      <div class="pagination-bar">
        <el-pagination
          background
          layout="total, sizes, prev, pager, next, jumper"
          :total="total"
          :current-page.sync="query.page"
          :page-size.sync="query.pageSize"
          :page-sizes="[10, 20, 50, 100]"
          @current-change="loadData"
          @size-change="handleSearch"
        />
      </div>
    </div>

    <el-dialog :title="form.id ? 'Edit Material' : 'New Material'" :visible.sync="dialogVisible" width="640px">
      <el-form ref="form" :model="form" :rules="rules" label-width="100px">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="Material Code" prop="code">
              <el-input v-model="form.code" placeholder="e.g. M-1001" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="Material Name" prop="name">
              <el-input v-model="form.name" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="Spec">
              <el-input v-model="form.spec" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="Category" prop="categoryId">
              <el-select v-model="form.categoryId" placeholder="Select category" style="width: 100%">
                <el-option v-for="item in categories" :key="item.id" :label="item.name" :value="item.id" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="Unit" prop="unit">
              <el-input v-model="form.unit" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="Brand">
              <el-input v-model="form.brand" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="Unit Price" prop="price">
              <el-input-number v-model="form.price" :min="0" :precision="2" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="Safety Stock">
              <el-input-number v-model="form.safetyStock" :min="0" :precision="2" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="Status">
              <el-switch v-model="form.status" :active-value="1" :inactive-value="0" active-text="Enabled" inactive-text="Disabled" />
            </el-form-item>
          </el-col>
          <el-col :span="24">
            <el-form-item label="Remarks">
              <el-input v-model="form.remark" type="textarea" :rows="2" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>
      <span slot="footer">
        <el-button @click="dialogVisible = false">Cancel</el-button>
        <el-button type="primary" :loading="saving" @click="submit">Save</el-button>
      </span>
    </el-dialog>
  </div>
</template>

<script>
import { materialApi, categoryApi } from '@/api/masterdata'

const emptyForm = () => ({
  id: null,
  code: '',
  name: '',
  spec: '',
  categoryId: undefined,
  unit: 'pcs',
  brand: '',
  price: 0,
  safetyStock: 0,
  status: 1,
  remark: ''
})

export default {
  name: 'Materials',
  data() {
    return {
      loading: false,
      saving: false,
      dialogVisible: false,
      list: [],
      total: 0,
      categories: [],
      query: { page: 1, pageSize: 20, keyword: '', categoryId: undefined, status: undefined },
      form: emptyForm(),
      rules: {
        code: [{ required: true, message: 'Enter material code', trigger: 'blur' }],
        name: [{ required: true, message: 'Enter material name', trigger: 'blur' }],
        categoryId: [{ required: true, message: 'Select material category', trigger: 'change' }]
      }
    }
  },
  created() {
    this.loadCategories()
    this.loadData()
  },
  methods: {
    async loadCategories() {
      this.categories = await categoryApi.list()
    },
    async loadData() {
      this.loading = true
      try {
        const result = await materialApi.page(this.query)
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
      this.query = { page: 1, pageSize: 20, keyword: '', categoryId: undefined, status: undefined }
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
            await materialApi.update(this.form.id, this.form)
          } else {
            await materialApi.create(this.form)
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
      this.$confirm(`Delete material "${row.name}"?`, 'Confirm', { type: 'warning' })
        .then(async () => {
          await materialApi.remove(row.id)
          this.$message.success('Deleted successfully')
          this.loadData()
        })
        .catch(() => {})
    }
  }
}
</script>
