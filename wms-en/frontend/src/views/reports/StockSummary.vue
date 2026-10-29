<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="loadData">
          <el-form-item>
            <el-input v-model="query.keyword" placeholder="Material Code / Name" clearable style="width: 220px" />
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.warehouseId" placeholder="All Warehouses" clearable style="width: 160px">
              <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" icon="el-icon-search" @click="loadData">Search</el-button>
            <el-button icon="el-icon-refresh" @click="handleReset">Reset</el-button>
          </el-form-item>
        </el-form>
        <div class="total-bar">
          Total Stock Value: <strong>{{ formatNumber(totalAmount) }}</strong> CNY
        </div>
      </div>

      <el-table v-loading="loading" :data="filteredList" border stripe show-summary :summary-method="summaryMethod">
        <el-table-column prop="code" label="Material Code" width="140" />
        <el-table-column prop="name" label="Material Name" min-width="160" show-overflow-tooltip />
        <el-table-column prop="spec" label="Spec" min-width="130" show-overflow-tooltip />
        <el-table-column prop="categoryName" label="Category" width="120" />
        <el-table-column prop="unit" label="Unit" width="70" align="center" />
        <el-table-column prop="onHandQty" label="On-Hand Qty" width="120" align="right" />
        <el-table-column prop="lockedQty" label="Locked Qty" width="110" align="right" />
        <el-table-column prop="availableQty" label="Available Qty" width="130" align="right" />
        <el-table-column prop="avgCost" label="Avg. Cost" width="110" align="right" />
        <el-table-column prop="amount" label="Stock Value" width="120" align="right" />
        <el-table-column prop="safetyStock" label="Safety Stock" width="120" align="right" />
        <el-table-column prop="alert" label="Alert" width="90" align="center">
          <template slot-scope="{ row }">
            <el-tag v-if="row.alert" type="danger" size="mini">Shortage</el-tag>
            <span v-else class="text-muted">OK</span>
          </template>
        </el-table-column>
      </el-table>
    </div>
  </div>
</template>

<script>
import { reportApi } from '@/api/inventory'
import { warehouseApi } from '@/api/masterdata'

export default {
  name: 'StockSummaryReport',
  data() {
    return {
      loading: false,
      list: [],
      warehouses: [],
      query: { keyword: '', warehouseId: undefined }
    }
  },
  computed: {
    filteredList() {
      const keyword = this.query.keyword.trim().toLowerCase()
      if (!keyword) return this.list
      return this.list.filter(
        item => item.code.toLowerCase().includes(keyword) || item.name.toLowerCase().includes(keyword)
      )
    },
    totalAmount() {
      return this.filteredList.reduce((sum, item) => sum + Number(item.amount || 0), 0)
    }
  },
  created() {
    this.loadWarehouses()
    this.loadData()
  },
  methods: {
    formatNumber(value) {
      return Number(value || 0).toLocaleString('zh-CN', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
    },
    async loadWarehouses() {
      this.warehouses = await warehouseApi.options()
    },
    async loadData() {
      this.loading = true
      try {
        this.list = await reportApi.stockSummary({ warehouseId: this.query.warehouseId })
      } finally {
        this.loading = false
      }
    },
    handleReset() {
      this.query = { keyword: '', warehouseId: undefined }
      this.loadData()
    },
    summaryMethod({ columns, data }) {
      const sums = []
      columns.forEach((column, index) => {
        if (index === 0) {
          sums[index] = 'Total'
          return
        }
        const numericColumns = ['onHandQty', 'lockedQty', 'availableQty', 'amount']
        if (numericColumns.includes(column.property)) {
          sums[index] = this.formatNumber(data.reduce((sum, row) => sum + Number(row[column.property] || 0), 0))
          return
        }
        sums[index] = ''
      })
      return sums
    }
  }
}
</script>

<style scoped>
.total-bar {
  color: #606266;
  font-size: 13px;
}
.total-bar strong {
  color: #409eff;
  font-size: 16px;
}
</style>
