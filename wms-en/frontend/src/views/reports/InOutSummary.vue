<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="loadData">
          <el-form-item>
            <el-date-picker
              v-model="dateRange"
              type="daterange"
              value-format="yyyy-MM-dd"
              range-separator="to"
              start-placeholder="Start Date"
              end-placeholder="End Date"
              style="width: 260px"
            />
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
      </div>

      <el-table v-loading="loading" :data="list" border stripe show-summary :summary-method="summaryMethod">
        <el-table-column prop="code" label="Material Code" width="140" />
        <el-table-column prop="name" label="Material Name" min-width="180" show-overflow-tooltip />
        <el-table-column prop="unit" label="Unit" width="80" align="center" />
        <el-table-column prop="beginQty" label="Opening Qty" width="130" align="right" />
        <el-table-column prop="inQty" label="Inbound Qty" width="130" align="right">
          <template slot-scope="{ row }"><span class="text-success">{{ row.inQty }}</span></template>
        </el-table-column>
        <el-table-column prop="outQty" label="Outbound Qty" width="140" align="right">
          <template slot-scope="{ row }"><span class="text-danger">{{ row.outQty }}</span></template>
        </el-table-column>
        <el-table-column prop="endQty" label="Closing Qty" width="130" align="right" />
      </el-table>
    </div>
  </div>
</template>

<script>
import { reportApi } from '@/api/inventory'
import { warehouseApi } from '@/api/masterdata'

const format = date => date.toISOString().substring(0, 10)

export default {
  name: 'InOutSummaryReport',
  data() {
    const end = new Date()
    const start = new Date()
    start.setDate(start.getDate() - 29)
    return {
      loading: false,
      list: [],
      warehouses: [],
      dateRange: [format(start), format(end)],
      query: { warehouseId: undefined }
    }
  },
  created() {
    this.loadWarehouses()
    this.loadData()
  },
  methods: {
    async loadWarehouses() {
      this.warehouses = await warehouseApi.options()
    },
    async loadData() {
      this.loading = true
      try {
        const params = { warehouseId: this.query.warehouseId }
        if (this.dateRange && this.dateRange.length === 2) {
          params.startDate = this.dateRange[0]
          params.endDate = this.dateRange[1]
        }
        this.list = await reportApi.inOutSummary(params)
      } finally {
        this.loading = false
      }
    },
    handleReset() {
      const end = new Date()
      const start = new Date()
      start.setDate(start.getDate() - 29)
      this.dateRange = [format(start), format(end)]
      this.query = { warehouseId: undefined }
      this.loadData()
    },
    summaryMethod({ columns, data }) {
      const sums = []
      columns.forEach((column, index) => {
        if (index === 0) {
          sums[index] = 'Total'
          return
        }
        if (['beginQty', 'inQty', 'outQty', 'endQty'].includes(column.property)) {
          sums[index] = data.reduce((sum, row) => sum + Number(row[column.property] || 0), 0)
          return
        }
        sums[index] = ''
      })
      return sums
    }
  }
}
</script>
