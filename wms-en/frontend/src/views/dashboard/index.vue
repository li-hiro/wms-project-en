<template>
  <div class="page-container">
    <el-row :gutter="16">
      <el-col :xs="12" :sm="8" :md="6" v-for="card in cards" :key="card.label">
        <div class="page-card stat-card">
          <div class="stat-icon" :style="{ color: card.color }">
            <i :class="card.icon"></i>
          </div>
          <div>
            <div class="stat-label">{{ card.label }}</div>
            <div class="stat-value">{{ card.value }}</div>
          </div>
        </div>
      </el-col>
    </el-row>

    <el-row :gutter="16" class="chart-row">
      <el-col :md="15">
        <div class="page-card">
          <div class="card-title">Last 7 Days In/Out Trend</div>
          <div ref="trendChart" class="chart"></div>
        </div>
      </el-col>
      <el-col :md="9">
        <div class="page-card">
          <div class="card-title">Stock Value by Category</div>
          <div ref="pieChart" class="chart"></div>
        </div>
      </el-col>
    </el-row>

    <div class="page-card">
      <div class="card-title">Recent Transactions</div>
      <el-table :data="dashboard.recentTransactions" border stripe>
        <el-table-column prop="occurredAt" label="Occurred At" min-width="150">
          <template slot-scope="{ row }">{{ formatTime(row.occurredAt) }}</template>
        </el-table-column>
        <el-table-column prop="typeName" label="Type" width="110">
          <template slot-scope="{ row }">{{ typeName(row.type) }}</template>
        </el-table-column>
        <el-table-column prop="materialCode" label="Material Code" width="140" />
        <el-table-column prop="materialName" label="Material Name" min-width="140" show-overflow-tooltip />
        <el-table-column prop="warehouseName" label="Warehouse" width="120" />
        <el-table-column prop="quantity" label="Qty" width="100" align="right">
          <template slot-scope="{ row }">
            <span :class="row.quantity >= 0 ? 'text-success' : 'text-danger'">{{ row.quantity }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="balanceQty" label="Balance" width="100" align="right" />
        <el-table-column prop="operatorName" label="Operator" width="110" />
      </el-table>
    </div>
  </div>
</template>

<script>
import * as echarts from 'echarts'
import { reportApi } from '@/api/inventory'

const TRANSACTION_TYPES = {
  1: 'Inbound',
  2: 'Outbound',
  3: 'Transfer In',
  4: 'Transfer Out',
  5: 'Lock',
  6: 'Unlock',
  7: 'Stocktake Adjustment',
  8: 'Opening'
}

export default {
  name: 'Dashboard',
  data() {
    return {
      dashboard: {
        trend: [],
        categoryDistribution: [],
        recentTransactions: []
      }
    }
  },
  computed: {
    cards() {
      const data = this.dashboard
      return [
        { label: 'Materials', value: data.materialCount || 0, icon: 'el-icon-box', color: '#409eff' },
        { label: 'Total Stock', value: this.formatNumber(data.stockQty), icon: 'el-icon-s-grid', color: '#67c23a' },
        { label: 'Stock Value (CNY)', value: this.formatNumber(data.stockAmount), icon: 'el-icon-money', color: '#e6a23c' },
        { label: 'Stock Alerts', value: data.alertCount || 0, icon: 'el-icon-warning-outline', color: '#f56c6c' },
        { label: "Today's Inbound", value: data.todayInboundOrders || 0, icon: 'el-icon-download', color: '#409eff' },
        { label: "Today's Outbound", value: data.todayOutboundOrders || 0, icon: 'el-icon-upload2', color: '#67c23a' },
        { label: 'Pending Inbound', value: data.pendingInbound || 0, icon: 'el-icon-time', color: '#e6a23c' },
        { label: 'Pending Outbound', value: data.pendingOutbound || 0, icon: 'el-icon-time', color: '#909399' }
      ]
    }
  },
  mounted() {
    this.loadData()
    window.addEventListener('resize', this.resizeCharts)
  },
  beforeDestroy() {
    window.removeEventListener('resize', this.resizeCharts)
    this.trendChart && this.trendChart.dispose()
    this.pieChart && this.pieChart.dispose()
  },
  methods: {
    async loadData() {
      this.dashboard = await reportApi.dashboard()
      this.$nextTick(() => {
        this.renderTrend()
        this.renderPie()
      })
    },
    renderTrend() {
      if (!this.$refs.trendChart) return
      this.trendChart = this.trendChart || echarts.init(this.$refs.trendChart)
      const trend = this.dashboard.trend || []
      this.trendChart.setOption({
        tooltip: { trigger: 'axis' },
        legend: { data: ['Inbound Qty', 'Outbound Qty'] },
        grid: { left: 40, right: 20, top: 40, bottom: 30 },
        xAxis: { type: 'category', data: trend.map(x => x.date) },
        yAxis: { type: 'value' },
        series: [
          { name: 'Inbound Qty', type: 'line', smooth: true, data: trend.map(x => x.inboundQty) },
          { name: 'Outbound Qty', type: 'line', smooth: true, data: trend.map(x => x.outboundQty) }
        ]
      })
    },
    renderPie() {
      if (!this.$refs.pieChart) return
      this.pieChart = this.pieChart || echarts.init(this.$refs.pieChart)
      const items = this.dashboard.categoryDistribution || []
      this.pieChart.setOption({
        tooltip: { trigger: 'item', formatter: '{b}: {c} CNY ({d}%)' },
        series: [
          {
            name: 'Stock Value',
            type: 'pie',
            radius: ['40%', '65%'],
            data: items.length ? items : [{ name: 'No Stock', value: 1 }]
          }
        ]
      })
    },
    resizeCharts() {
      this.trendChart && this.trendChart.resize()
      this.pieChart && this.pieChart.resize()
    },
    formatNumber(value) {
      const number = Number(value || 0)
      return number.toLocaleString('zh-CN', { maximumFractionDigits: 2 })
    },
    formatTime(value) {
      if (!value) return '—'
      return value.replace('T', ' ').substring(0, 19)
    },
    typeName(type) {
      return TRANSACTION_TYPES[type] || 'Other'
    }
  }
}
</script>

<style scoped>
.stat-card {
  display: flex;
  align-items: center;
  margin-bottom: 16px;
}

.stat-icon {
  font-size: 30px;
  margin-right: 14px;
}

.stat-label {
  color: #909399;
  font-size: 12px;
}

.stat-value {
  font-size: 20px;
  font-weight: 600;
  margin-top: 4px;
}

.chart-row {
  margin-bottom: 16px;
}

.card-title {
  font-size: 14px;
  font-weight: 600;
  margin-bottom: 12px;
}

.chart {
  height: 300px;
}
</style>
