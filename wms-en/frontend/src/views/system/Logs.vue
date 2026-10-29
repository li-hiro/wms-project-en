<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="handleSearch">
          <el-form-item>
            <el-select v-model="query.module" placeholder="Module" clearable style="width: 180px">
              <el-option v-for="item in modules" :key="item" :label="item" :value="item" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.success" placeholder="Result" clearable style="width: 130px">
              <el-option label="Success" :value="true" />
              <el-option label="Failed" :value="false" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-input v-model="query.keyword" placeholder="Operator / Path" clearable style="width: 200px" />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" icon="el-icon-search" @click="handleSearch">Search</el-button>
            <el-button icon="el-icon-refresh" @click="handleReset">Reset</el-button>
          </el-form-item>
        </el-form>
        <el-button v-permission="'system:log:view'" type="danger" plain icon="el-icon-delete" @click="handleClear">Clear Logs Older Than 30 Days</el-button>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="createdAt" label="Time" width="160">
          <template slot-scope="{ row }">{{ formatDateTime(row.createdAt) }}</template>
        </el-table-column>
        <el-table-column prop="userName" label="Operator" width="110" />
        <el-table-column prop="module" label="Module" width="110" />
        <el-table-column prop="action" label="Action" width="130" show-overflow-tooltip />
        <el-table-column prop="method" label="Method" width="100" />
        <el-table-column prop="path" label="Path" min-width="220" show-overflow-tooltip />
        <el-table-column prop="ip" label="IP" width="130" />
        <el-table-column prop="durationMs" label="Duration (ms)" width="120" align="right" />
        <el-table-column prop="success" label="Result" width="80" align="center">
          <template slot-scope="{ row }">
            <el-tag :type="row.success ? 'success' : 'danger'" size="mini">{{ row.success ? 'Success' : 'Failed' }}</el-tag>
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
          :page-sizes="[20, 50, 100]"
          @current-change="loadData"
          @size-change="handleSearch"
        />
      </div>
    </div>
  </div>
</template>

<script>
import { logApi } from '@/api/system'

export default {
  name: 'SystemLogs',
  data() {
    return {
      loading: false,
      list: [],
      total: 0,
      modules: ['Material Management', 'Warehouse Management', 'Inbound Operations', 'Outbound Operations', 'Stocktake', 'Stock Transfers', 'Inventory Management', 'Reports', 'System Management', 'Plugin Management'],
      query: { page: 1, pageSize: 20, keyword: '', module: undefined, success: undefined }
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
        const result = await logApi.page(this.query)
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
      this.query = { page: 1, pageSize: 20, keyword: '', module: undefined, success: undefined }
      this.loadData()
    },
    handleClear() {
      this.$confirm('Clear all operation logs older than 30 days?', 'Confirm', { type: 'warning' })
        .then(async () => {
          const count = await logApi.clear(30)
          this.$message.success(`Cleared ${count} log entries`)
          this.loadData()
        })
        .catch(() => {})
    }
  }
}
</script>
