<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="handleSearch">
          <el-form-item>
            <el-input v-model="query.keyword" placeholder="Username / Nickname" clearable style="width: 200px" />
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.roleId" placeholder="Role" clearable style="width: 150px">
              <el-option v-for="item in roles" :key="item.id" :label="item.name" :value="item.id" />
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
        <el-button v-permission="'system:user:edit'" type="primary" icon="el-icon-plus" @click="openCreate">New User</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="userName" label="Username" width="140" />
        <el-table-column prop="nickName" label="Nickname" width="140" />
        <el-table-column label="Roles" min-width="180">
          <template slot-scope="{ row }">
            <el-tag v-for="name in row.roleNames" :key="name" size="mini" style="margin-right: 4px">{{ name }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="email" label="Email" min-width="180" show-overflow-tooltip />
        <el-table-column prop="phone" label="Phone" width="130" />
        <el-table-column prop="lastLoginAt" label="Last Login" width="160">
          <template slot-scope="{ row }">{{ formatDateTime(row.lastLoginAt) }}</template>
        </el-table-column>
        <el-table-column prop="status" label="Status" width="90" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'info'" size="mini">
              {{ row.status === 1 ? 'Enabled' : 'Disabled' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="Actions" width="280" fixed="right">
          <template slot-scope="{ row }">
            <el-button v-permission="'system:user:edit'" type="text" @click="openEdit(row)">Edit</el-button>
            <el-button v-permission="'system:user:edit'" type="text" @click="handleResetPassword(row)">Reset Password</el-button>
            <el-button v-permission="'system:user:edit'" type="text" @click="handleToggle(row)">
              {{ row.status === 1 ? 'Disable' : 'Enable' }}
            </el-button>
            <el-button v-permission="'system:user:edit'" type="text" class="text-danger" @click="handleDelete(row)">Delete</el-button>
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

    <el-dialog :title="form.id ? 'Edit User' : 'New User'" :visible.sync="dialogVisible" width="560px">
      <el-form ref="form" :model="form" :rules="rules" label-width="90px">
        <el-form-item label="Username" prop="userName">
          <el-input v-model="form.userName" :disabled="!!form.id" />
        </el-form-item>
        <el-form-item label="Nickname" prop="nickName">
          <el-input v-model="form.nickName" />
        </el-form-item>
        <el-form-item v-if="!form.id" label="Initial Password" prop="password">
          <el-input v-model="form.password" type="password" show-password placeholder="At least 6 characters" />
        </el-form-item>
        <el-form-item label="Roles" prop="roleIds">
          <el-select v-model="form.roleIds" multiple style="width: 100%">
            <el-option v-for="item in roles" :key="item.id" :label="item.name" :value="item.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="Email">
          <el-input v-model="form.email" />
        </el-form-item>
        <el-form-item label="Phone">
          <el-input v-model="form.phone" />
        </el-form-item>
        <el-form-item label="Status">
          <el-switch v-model="form.status" :active-value="1" :inactive-value="0" active-text="Enabled" inactive-text="Disabled" />
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
import { userApi, roleApi } from '@/api/system'

const emptyForm = () => ({
  id: null,
  userName: '',
  nickName: '',
  password: '',
  email: '',
  phone: '',
  status: 1,
  roleIds: []
})

export default {
  name: 'SystemUsers',
  data() {
    return {
      loading: false,
      saving: false,
      dialogVisible: false,
      list: [],
      total: 0,
      roles: [],
      query: { page: 1, pageSize: 20, keyword: '', status: undefined, roleId: undefined },
      form: emptyForm(),
      rules: {
        userName: [{ required: true, message: 'Enter username', trigger: 'blur' }],
        nickName: [{ required: true, message: 'Enter nickname', trigger: 'blur' }],
        password: [
          { required: true, message: 'Enter initial password', trigger: 'blur' },
          { min: 6, message: 'Password must be at least 6 characters', trigger: 'blur' }
        ],
        roleIds: [{ type: 'array', required: true, message: 'Select at least one role', trigger: 'change' }]
      }
    }
  },
  created() {
    this.loadRoles()
    this.loadData()
  },
  methods: {
    formatDateTime(value) {
      return value ? value.replace('T', ' ').substring(0, 19) : '—'
    },
    async loadRoles() {
      this.roles = await roleApi.list()
    },
    async loadData() {
      this.loading = true
      try {
        const result = await userApi.page(this.query)
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
      this.query = { page: 1, pageSize: 20, keyword: '', status: undefined, roleId: undefined }
      this.loadData()
    },
    openCreate() {
      this.form = emptyForm()
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    openEdit(row) {
      this.form = {
        id: row.id,
        userName: row.userName,
        nickName: row.nickName,
        email: row.email,
        phone: row.phone,
        status: row.status,
        roleIds: row.roleIds || []
      }
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    submit() {
      this.$refs.form.validate(async valid => {
        if (!valid) return
        this.saving = true
        try {
          const payload = { ...this.form }
          if (payload.id) delete payload.password
          if (payload.id) {
            await userApi.update(payload.id, payload)
          } else {
            await userApi.create(payload)
          }
          this.$message.success('Saved successfully')
          this.dialogVisible = false
          this.loadData()
        } finally {
          this.saving = false
        }
      })
    },
    handleResetPassword(row) {
      this.$prompt('Enter a new password (leave blank to generate a random one)', `Reset password for "${row.userName}"`, {
        inputPlaceholder: 'New password, optional'
      })
        .then(async ({ value }) => {
          const password = await userApi.resetPassword(row.id, { password: value || null })
          this.$alert(`New password: ${password}`, 'Password Reset', { type: 'success' })
        })
        .catch(() => {})
    },
    handleToggle(row) {
      const next = row.status === 1 ? 0 : 1
      this.$confirm(`Are you sure you want to ${next === 1 ? 'enable' : 'disable'} user "${row.userName}"?`, 'Confirm', { type: 'warning' })
        .then(async () => {
          await userApi.toggleStatus(row.id, next)
          this.$message.success('Status updated')
          this.loadData()
        })
        .catch(() => {})
    },
    handleDelete(row) {
      this.$confirm(`Are you sure you want to delete user "${row.userName}"?`, 'Confirm', { type: 'warning' })
        .then(async () => {
          await userApi.remove(row.id)
          this.$message.success('Deleted successfully')
          this.loadData()
        })
        .catch(() => {})
    }
  }
}
</script>
