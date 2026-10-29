<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <span class="text-muted">Roles determine which menus and API permissions a user can access. The admin role has all permissions.</span>
        <el-button v-permission="'system:role:edit'" type="primary" icon="el-icon-plus" @click="openCreate">New Role</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="code" label="Role Code" width="150" />
        <el-table-column prop="name" label="Role Name" width="160" />
        <el-table-column prop="remark" label="Description" min-width="220" show-overflow-tooltip />
        <el-table-column prop="userCount" label="Users" width="90" align="center" />
        <el-table-column label="Permissions" width="110" align="center">
          <template slot-scope="{ row }">{{ row.code === 'admin' ? 'All' : row.permissionIds.length }}</template>
        </el-table-column>
        <el-table-column prop="status" label="Status" width="90" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="row.status === 1 ? 'success' : 'info'" size="mini">
              {{ row.status === 1 ? 'Enabled' : 'Disabled' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="Actions" width="180" fixed="right">
          <template slot-scope="{ row }">
            <el-button v-permission="'system:role:edit'" type="text" @click="openEdit(row)">Edit</el-button>
            <el-button
              v-permission="'system:role:edit'"
              type="text"
              class="text-danger"
              :disabled="row.code === 'admin'"
              @click="handleDelete(row)"
            >Delete</el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <el-dialog :title="form.id ? 'Edit Role' : 'New Role'" :visible.sync="dialogVisible" width="760px">
      <el-form ref="form" :model="form" :rules="rules" label-width="90px">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="Role Code" prop="code">
              <el-input v-model="form.code" :disabled="!!form.id" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="Role Name" prop="name">
              <el-input v-model="form.name" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="Description">
          <el-input v-model="form.remark" />
        </el-form-item>
        <el-form-item label="Status">
          <el-switch v-model="form.status" :active-value="1" :inactive-value="0" active-text="Enabled" inactive-text="Disabled" />
        </el-form-item>
        <el-form-item label="Menu Access">
          <el-tree
            ref="menuTree"
            :data="menuTree"
            show-checkbox
            node-key="id"
            :props="{ label: 'name', children: 'children' }"
            class="menu-tree"
          />
        </el-form-item>
        <el-form-item label="API Permissions">
          <el-checkbox-group v-model="form.permissionIds" class="permission-group">
            <el-checkbox v-for="item in permissions" :key="item.id" :label="item.id" class="permission-item">
              {{ item.name }}
            </el-checkbox>
          </el-checkbox-group>
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
import { roleApi, menuApi } from '@/api/system'

const emptyForm = () => ({
  id: null,
  code: '',
  name: '',
  remark: '',
  status: 1,
  permissionIds: [],
  menuIds: []
})

export default {
  name: 'SystemRoles',
  data() {
    return {
      loading: false,
      saving: false,
      dialogVisible: false,
      list: [],
      permissions: [],
      menuTree: [],
      form: emptyForm(),
      rules: {
        code: [{ required: true, message: 'Enter role code', trigger: 'blur' }],
        name: [{ required: true, message: 'Enter role name', trigger: 'blur' }]
      }
    }
  },
  created() {
    this.loadData()
    this.loadPermissions()
    this.loadMenus()
  },
  methods: {
    async loadData() {
      this.loading = true
      try {
        this.list = await roleApi.list()
      } finally {
        this.loading = false
      }
    },
    async loadPermissions() {
      this.permissions = await roleApi.permissions()
    },
    async loadMenus() {
      this.menuTree = await menuApi.tree()
    },
    openCreate() {
      this.form = emptyForm()
      this.dialogVisible = true
      this.$nextTick(() => {
        this.$refs.form && this.$refs.form.clearValidate()
        this.$refs.menuTree && this.$refs.menuTree.setCheckedKeys([])
      })
    },
    openEdit(row) {
      this.form = {
        id: row.id,
        code: row.code,
        name: row.name,
        remark: row.remark,
        status: row.status,
        permissionIds: [...(row.permissionIds || [])],
        menuIds: [...(row.menuIds || [])]
      }
      this.dialogVisible = true
      this.$nextTick(() => {
        this.$refs.form && this.$refs.form.clearValidate()
        this.$refs.menuTree && this.$refs.menuTree.setCheckedKeys(this.form.menuIds)
      })
    },
    submit() {
      this.$refs.form.validate(async valid => {
        if (!valid) return
        const checked = this.$refs.menuTree.getCheckedKeys()
        const halfChecked = this.$refs.menuTree.getHalfCheckedKeys()
        this.form.menuIds = checked.concat(halfChecked)
        this.saving = true
        try {
          if (this.form.id) {
            await roleApi.update(this.form.id, this.form)
          } else {
            await roleApi.create(this.form)
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
      this.$confirm(`Are you sure you want to delete role "${row.name}"?`, 'Confirm', { type: 'warning' })
        .then(async () => {
          await roleApi.remove(row.id)
          this.$message.success('Deleted successfully')
          this.loadData()
        })
        .catch(() => {})
    }
  }
}
</script>

<style scoped>
.menu-tree {
  max-height: 220px;
  overflow: auto;
  border: 1px solid #ebeef5;
  border-radius: 4px;
  padding: 6px 10px;
  width: 100%;
}
.permission-group {
  max-height: 200px;
  overflow: auto;
  border: 1px solid #ebeef5;
  border-radius: 4px;
  padding: 8px 10px;
  width: 100%;
}
.permission-item {
  width: 30%;
  margin-right: 0;
}
</style>
