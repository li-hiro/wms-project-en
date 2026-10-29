<template>
  <div class="page-container">
    <el-row :gutter="16" class="summary-row">
      <el-col :xs="12" :sm="6" v-for="item in summaries" :key="item.warehouseId">
        <div class="page-card summary-card">
          <div class="summary-title">{{ item.warehouseName }}</div>
          <div class="summary-value">{{ formatNumber(item.onHandQty) }}</div>
          <div class="summary-sub">
            Available {{ formatNumber(item.availableQty) }} · Locked {{ formatNumber(item.lockedQty) }}
          </div>
        </div>
      </el-col>
    </el-row>

    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="handleSearch">
          <el-form-item>
            <el-input v-model="query.keyword" placeholder="Material code / name / batch" clearable style="width: 230px" />
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.warehouseId" placeholder="Warehouse" clearable style="width: 150px">
              <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-checkbox v-model="query.onlyInStock">In stock only</el-checkbox>
          </el-form-item>
          <el-form-item>
            <el-checkbox v-model="query.onlyAlerts">Alerts only</el-checkbox>
          </el-form-item>
          <el-form-item>
            <el-button type="primary" icon="el-icon-search" @click="handleSearch">Search</el-button>
            <el-button icon="el-icon-refresh" @click="handleReset">Reset</el-button>
          </el-form-item>
        </el-form>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="materialCode" label="Material Code" width="130" />
        <el-table-column prop="materialName" label="Material Name" min-width="160" show-overflow-tooltip />
        <el-table-column prop="spec" label="Spec" min-width="130" show-overflow-tooltip />
        <el-table-column prop="warehouseName" label="Warehouse" width="120" />
        <el-table-column prop="locationCode" label="Location" width="110" />
        <el-table-column prop="batchNo" label="Batch No." width="120" />
        <el-table-column prop="onHandQty" label="On-hand Qty" width="100" align="right" />
        <el-table-column prop="lockedQty" label="Locked Qty" width="100" align="right" />
        <el-table-column prop="availableQty" label="Available Qty" width="120" align="right" />
        <el-table-column prop="unit" label="Unit" width="70" align="center" />
        <el-table-column prop="unitCost" label="Unit Cost" width="100" align="right" />
        <el-table-column prop="amount" label="Stock Value" width="110" align="right" />
        <el-table-column prop="alert" label="Alert" width="110" align="center">
          <template slot-scope="{ row }">
            <el-tag v-if="row.alert" type="danger" size="mini">Out of Stock</el-tag>
            <span v-else class="text-muted">Normal</span>
          </template>
        </el-table-column>
        <el-table-column prop="lastInAt" label="Last Inbound" width="150">
          <template slot-scope="{ row }">{{ formatDateTime(row.lastInAt) }}</template>
        </el-table-column>
      </el-table>

      <div class="pagination-bar">
        <el-pagination
          background
          layout="total, sizes, prev, pager, next"
          :total="total"
          :current-page.sync="query.page"
          :page-size.sync="query.pageSize"
          :page-sizes="[10, 20, 50, 100]"
          @current-change="loadData"
          @size-change="handleSearch"
        />
      </div>
    </div>
  </div>
</template>

<script>
import { inventoryApi } from '@/api/inventory'
import { warehouseApi } from '@/api/masterdata'

export default {
  name: 'InventoryStock',
  data() {
    return {
      loading: false,
      list: [],
      total: 0,
      summaries: [],
      warehouses: [],
      query: {
        page: 1,
        pageSize: 20,
        keyword: '',
        warehouseId: undefined,
        onlyInStock: false,
        onlyAlerts: false
      }
    }
  },
  created() {
    this.loadWarehouses()
    this.loadSummary()
    this.loadData()
  },
  methods: {
    formatNumber(value) {
      return Number(value || 0).toLocaleString('zh-CN', { maximumFractionDigits: 2 })
    },
    formatDateTime(value) {
      return value ? value.replace('T', ' ').substring(0, 19) : '—'
    },
    async loadWarehouses() {
      this.warehouses = await warehouseApi.options()
    },
    async loadSummary() {
      this.summaries = await inventoryApi.summary()
    },
    async loadData() {
      this.loading = true
      try {
        const params = { ...this.query }
        if (!params.onlyInStock) delete params.onlyInStock
        if (!params.onlyAlerts) delete params.onlyAlerts
        const result = await inventoryApi.page(params)
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
      this.query = {
        page: 1,
        pageSize: 20,
        keyword: '',
        warehouseId: undefined,
        onlyInStock: false,
        onlyAlerts: false
      }
      this.loadData()
    }
  }
}
</script>

<style scoped>
.summary-row {
  margin-bottom: 16px;
}
.summary-card {
  margin-bottom: 16px;
}
.summary-title {
  color: #909399;
  font-size: 13px;
}
.summary-value {
  font-size: 22px;
  font-weight: 600;
  margin: 6px 0 4px;
}
.summary-sub {
  color: #909399;
  font-size: 12px;
}
</style>
