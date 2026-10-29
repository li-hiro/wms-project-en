<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <span class="text-muted">Material categories are used for statistics and filtering, with two levels supported</span>
        <el-button v-permission="'category:edit'" type="primary" icon="el-icon-plus" @click="openCreate">New Category</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe row-key="id">
        <el-table-column prop="name" label="Category Name" min-width="180" />
        <el-table-column prop="parentName" label="Parent Category" min-width="160">
          <template slot-scope="{ row }">{{ row.parentName || 'Top-level Category' }}</template>
        </el-table-column>
        <el-table-column prop="sort" label="Sort" width="90" align="center" />
        <el-table-column prop="status" label="Status" width="90" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'info'" size="mini">
              {{ row.status === 1 ? 'Enabled' : 'Disabled' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="remark" label="Remarks" min-width="200" show-overflow-tooltip />
        <el-table-column label="Actions" width="150">
          <template slot-scope="{ row }">
            <el-button v-permission="'category:edit'" type="text" @click="openEdit(row)">Edit</el-button>
            <el-button v-permission="'category:edit'" type="text" class="text-danger" @click="handleDelete(row)">Delete</el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <el-dialog :title="form.id ? 'Edit Category' : 'New Category'" :visible.sync="dialogVisible" width="480px">
      <el-form ref="form" :model="form" :rules="rules" label-width="90px">
        <el-form-item label="Category Name" prop="name">
          <el-input v-model="form.name" />
        </el-form-item>
        <el-form-item label="Parent Category">
          <el-select v-model="form.parentId" style="width: 100%">
            <el-option label="Top-level Category" :value="0" />
            <el-option v-for="item in parentOptions" :key="item.id" :label="item.name" :value="item.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="Sort">
          <el-input-number v-model="form.sort" :min="0" style="width: 100%" />
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
import { categoryApi } from '@/api/masterdata'

const emptyForm = () => ({ id: null, name: '', parentId: 0, sort: 0, status: 1, remark: '' })

export default {
  name: 'Categories',
  data() {
    return {
      loading: false,
      saving: false,
      dialogVisible: false,
      list: [],
      form: emptyForm(),
      rules: { name: [{ required: true, message: 'Enter category name', trigger: 'blur' }] }
    }
  },
  computed: {
    parentOptions() {
      return this.list.filter(item => !this.form.id || item.id !== this.form.id)
    }
  },
  created() {
    this.loadData()
  },
  methods: {
    async loadData() {
      this.loading = true
      try {
        this.list = await categoryApi.list()
      } finally {
        this.loading = false
      }
    },
    openCreate() {
      this.form = emptyForm()
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    openEdit(row) {
      this.form = { id: row.id, name: row.name, parentId: row.parentId, sort: row.sort, status: row.status, remark: row.remark }
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    submit() {
      this.$refs.form.validate(async valid => {
        if (!valid) return
        this.saving = true
        try {
          if (this.form.id) {
            await categoryApi.update(this.form.id, this.form)
          } else {
            await categoryApi.create(this.form)
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
      this.$confirm(`Delete category "${row.name}"?`, 'Confirm', { type: 'warning' })
        .then(async () => {
          await categoryApi.remove(row.id)
          this.$message.success('Deleted successfully')
          this.loadData()
        })
        .catch(() => {})
    }
  }
}
</script>
