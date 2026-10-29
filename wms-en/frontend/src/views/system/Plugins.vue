<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <span class="text-muted">Plugins extend capabilities such as stock alerts and third-party integrations without modifying the core application.</span>
        <el-button icon="el-icon-refresh" @click="loadData">Refresh</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="name" label="Plugin Name" min-width="150" />
        <el-table-column prop="id" label="Plugin ID" min-width="150" />
        <el-table-column prop="category" label="Category" width="130" />
        <el-table-column prop="description" label="Description" min-width="240" show-overflow-tooltip />
        <el-table-column prop="version" label="Version" width="90" align="center" />
        <el-table-column prop="author" label="Author" width="120" />
        <el-table-column prop="builtIn" label="Source" width="90" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="row.builtIn ? 'info' : 'warning'" size="mini">{{ row.builtIn ? 'Built-in' : 'External' }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="enabled" label="Enabled" width="90" align="center">
          <template slot-scope="{ row }">
            <el-switch
              :value="row.enabled"
              :disabled="!$store.getters.permissions.includes('*') && !$store.getters.permissions.includes('system:plugin:edit')"
              @change="value => handleToggle(row, value)"
            />
          </template>
        </el-table-column>
        <el-table-column label="Actions" width="180" fixed="right">
          <template slot-scope="{ row }">
            <el-button v-permission="'system:plugin:edit'" type="text" @click="openConfig(row)">Configure</el-button>
            <el-button v-permission="'system:plugin:edit'" type="text" @click="handleExecute(row)">Run</el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <el-dialog :title="`Plugin Settings - ${current.name || ''}`" :visible.sync="dialogVisible" width="560px">
      <el-form label-width="120px">
        <el-form-item v-for="(item, index) in settingRows" :key="index" :label="item.key || 'New Setting'">
          <div class="setting-row">
            <el-input v-if="!item.key" v-model="item.key" placeholder="Setting name" style="width: 42%" />
            <span v-else class="setting-key">{{ item.key }}</span>
            <el-input v-model="item.value" placeholder="Value" style="flex: 1" />
            <el-button type="text" class="text-danger" @click="settingRows.splice(index, 1)">Delete</el-button>
          </div>
        </el-form-item>
        <el-form-item>
          <el-button type="text" icon="el-icon-plus" @click="settingRows.push({ key: '', value: '' })">Add Setting</el-button>
        </el-form-item>
      </el-form>
      <span slot="footer">
        <el-button @click="dialogVisible = false">Cancel</el-button>
        <el-button type="primary" :loading="saving" @click="saveSettings">Save</el-button>
      </span>
    </el-dialog>

    <el-dialog title="Plugin Execution Result" :visible.sync="resultVisible" width="520px">
      <el-descriptions v-if="result" :column="1" border size="small">
        <el-descriptions-item label="Result">
          <el-tag :type="result.success ? 'success' : 'danger'" size="mini">{{ result.success ? 'Success' : 'Failed' }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="Message">{{ result.message }}</el-descriptions-item>
        <el-descriptions-item label="Duration">{{ result.durationMs }} ms</el-descriptions-item>
        <el-descriptions-item label="Executed At">{{ formatDateTime(result.executedAt) }}</el-descriptions-item>
      </el-descriptions>
    </el-dialog>
  </div>
</template>

<script>
import { pluginApi } from '@/api/system'

export default {
  name: 'SystemPlugins',
  data() {
    return {
      loading: false,
      saving: false,
      dialogVisible: false,
      resultVisible: false,
      list: [],
      current: {},
      settingRows: [],
      result: null
    }
  },
  created() {
    this.loadData()
  },
  methods: {
    formatDateTime(value) {
      return value ? value.replace('T', ' ').substring(0, 19) : '—'
    },
    async loadData() {
      this.loading = true
      try {
        this.list = await pluginApi.list()
      } finally {
        this.loading = false
      }
    },
    async handleToggle(row, enabled) {
      try {
        await pluginApi.update(row.id, { enabled, settings: row.settings || {} })
        this.$message.success(enabled ? 'Plugin enabled' : 'Plugin disabled')
        this.loadData()
      } catch (error) {
        this.loadData()
      }
    },
    openConfig(row) {
      this.current = row
      this.settingRows = Object.keys(row.settings || {}).map(key => ({ key, value: row.settings[key] }))
      this.dialogVisible = true
    },
    async saveSettings() {
      const settings = {}
      this.settingRows.forEach(item => {
        if (item.key) settings[item.key] = item.value
      })
      this.saving = true
      try {
        await pluginApi.update(this.current.id, { enabled: this.current.enabled, settings })
        this.$message.success('Settings saved')
        this.dialogVisible = false
        this.loadData()
      } finally {
        this.saving = false
      }
    },
    async handleExecute(row) {
      this.result = await pluginApi.execute(row.id)
      this.resultVisible = true
    }
  }
}
</script>

<style scoped>
.setting-row {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
}
.setting-key {
  width: 42%;
  color: #606266;
}
</style>
