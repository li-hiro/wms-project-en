<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <span class="text-muted">Menus control sidebar navigation and page access. Directories can contain submenus.</span>
        <el-button v-permission="'system:menu:edit'" type="primary" icon="el-icon-plus" @click="openCreate">New Menu</el-button>
      </div>

      <el-table v-loading="loading" :data="tree" border row-key="id" :tree-props="{ children: 'children' }">
        <el-table-column prop="name" label="Menu Name" min-width="180" />
        <el-table-column prop="type" label="Type" width="90" align="center">
          <template slot-scope="{ row }">{{ row.type === 0 ? 'Directory' : 'Menu' }}</template>
        </el-table-column>
        <el-table-column prop="path" label="Route Path" min-width="180" />
        <el-table-column prop="component" label="Component" min-width="180" show-overflow-tooltip />
        <el-table-column prop="permissionCode" label="Permission Code" min-width="160" />
        <el-table-column prop="icon" label="Icon" width="150">
          <template slot-scope="{ row }">
            <i v-if="row.icon" :class="row.icon"></i>
            <span class="text-muted">{{ row.icon }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="sort" label="Sort" width="80" align="center" />
        <el-table-column prop="visible" label="Visible" width="90" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="row.visible ? 'success' : 'info'" size="mini">{{ row.visible ? 'Visible' : 'Hidden' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column label="Actions" width="180" fixed="right">
          <template slot-scope="{ row }">
            <el-button v-permission="'system:menu:edit'" type="text" @click="openEdit(row)">Edit</el-button>
            <el-button v-permission="'system:menu:edit'" type="text" class="text-danger" @click="handleDelete(row)">Delete</el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <el-dialog :title="form.id ? 'Edit Menu' : 'New Menu'" :visible.sync="dialogVisible" width="600px">
      <el-form ref="form" :model="form" :rules="rules" label-width="90px">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="Menu Name" prop="name">
              <el-input v-model="form.name" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="Menu Type" prop="type">
              <el-radio-group v-model="form.type">
                <el-radio :label="0">Directory</el-radio>
                <el-radio :label="1">Menu</el-radio>
              </el-radio-group>
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="Parent Menu">
          <el-select v-model="form.parentId" style="width: 100%">
            <el-option label="Top-Level Menu" :value="0" />
            <el-option v-for="item in parentOptions" :key="item.id" :label="item.name" :value="item.id" />
          </el-select>
        </el-form-item>
        <el-form-item label="Route Path" prop="path">
          <el-input v-model="form.path" placeholder="e.g. /master/materials" />
        </el-form-item>
        <el-form-item label="Component">
          <el-input v-model="form.component" placeholder="e.g. master/Materials" />
        </el-form-item>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="Permission Code">
              <el-input v-model="form.permissionCode" placeholder="e.g. material:view" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="Icon">
              <el-input v-model="form.icon" placeholder="e.g. el-icon-box" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="Sort">
              <el-input-number v-model="form.sort" :min="0" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="Visible">
              <el-switch v-model="form.visible" active-text="Visible" inactive-text="Hidden" />
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
import { menuApi } from '@/api/system'

const emptyForm = () => ({
  id: null,
  name: '',
  path: '',
  component: '',
  icon: '',
  permissionCode: '',
  parentId: 0,
  sort: 0,
  type: 1,
  visible: true
})

export default {
  name: 'SystemMenus',
  data() {
    return {
      loading: false,
      saving: false,
      dialogVisible: false,
      tree: [],
      flat: [],
      form: emptyForm(),
      rules: {
        name: [{ required: true, message: 'Enter menu name', trigger: 'blur' }],
        path: [{ required: true, message: 'Enter route path', trigger: 'blur' }]
      }
    }
  },
  computed: {
    parentOptions() {
      return this.flat.filter(item => !this.form.id || item.id !== this.form.id)
    }
  },
  created() {
    this.loadData()
  },
  methods: {
    flatten(items, result = []) {
      items.forEach(item => {
        result.push(item)
        if (item.children && item.children.length) {
          this.flatten(item.children, result)
        }
      })
      return result
    },
    async loadData() {
      this.loading = true
      try {
        this.tree = await menuApi.tree()
        this.flat = this.flatten(this.tree)
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
      this.form = {
        id: row.id,
        name: row.name,
        path: row.path,
        component: row.component,
        icon: row.icon,
        permissionCode: row.permissionCode,
        parentId: row.parentId,
        sort: row.sort,
        type: row.type,
        visible: row.visible
      }
      this.dialogVisible = true
      this.$nextTick(() => this.$refs.form && this.$refs.form.clearValidate())
    },
    submit() {
      this.$refs.form.validate(async valid => {
        if (!valid) return
        this.saving = true
        try {
          if (this.form.id) {
            await menuApi.update(this.form.id, this.form)
          } else {
            await menuApi.create(this.form)
          }
          this.$message.success('Saved successfully')
          this.dialogVisible = false
          await this.loadData()
        } finally {
          this.saving = false
        }
      })
    },
    handleDelete(row) {
      this.$confirm(`Are you sure you want to delete menu "${row.name}"?`, 'Confirm', { type: 'warning' })
        .then(async () => {
          await menuApi.remove(row.id)
          this.$message.success('Deleted successfully')
          await this.loadData()
        })
        .catch(() => {})
    }
  }
}
</script>
