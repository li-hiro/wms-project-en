<template>
  <div class="page-container">
    <div class="page-card">
      <div class="table-toolbar">
        <el-form :inline="true" :model="query" @submit.native.prevent="handleSearch">
          <el-form-item>
            <el-select v-model="query.type" placeholder="Type" clearable style="width: 140px">
              <el-option v-for="item in typeOptions" :key="item.value" :label="item.label" :value="item.value" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-input v-model="query.keyword" placeholder="Material / ref. order no." clearable style="width: 220px" />
          </el-form-item>
          <el-form-item>
            <el-select v-model="query.warehouseId" placeholder="Warehouse" clearable style="width: 150px">
              <el-option v-for="item in warehouses" :key="item.id" :label="item.name" :value="item.id" />
            </el-select>
          </el-form-item>
          <el-form-item>
            <el-date-picker
              v-model="dateRange"
              type="daterange"
              value-format="yyyy-MM-dd"
              range-separator="to"
              start-placeholder="Start date"
              end-placeholder="End date"
              style="width: 260px"
            />
          </el-form-item>
          <el-form-item>
            <el-button type="primary" icon="el-icon-search" @click="handleSearch">Search</el-button>
            <el-button icon="el-icon-refresh" @click="handleReset">Reset</el-button>
          </el-form-item>
        </el-form>
      </div>

      <el-table v-loading="loading" :data="list" border stripe>
        <el-table-column prop="occurredAt" label="Occurred At" width="160">
          <template slot-scope="{ row }">{{ formatDateTime(row.occurredAt) }}</template>
        </el-table-column>
        <el-table-column prop="type" label="Type" width="130">
          <template slot-scope="{ row }">
            <el-tag :type="typeTagType(row.type)" size="mini">{{ typeName(row.type) }}</el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="materialCode" label="Material Code" width="130" />
        <el-table-column prop="materialName" label="Material Name" min-width="150" show-overflow-tooltip />
        <el-table-column prop="warehouseName" label="Warehouse" width="120" />
        <el-table-column prop="locationCode" label="Location" width="110" />
        <el-table-column prop="batchNo" label="Batch No." width="120" />
        <el-table-column prop="quantity" label="Change Qty" width="100" align="right">
          <template slot-scope="{ row }">
            <span :class="row.quantity >= 0 ? 'text-success' : 'text-danger'">{{ row.quantity }}</span>
          </template>
        </el-table-column>
        <el-table-column prop="lockedQuantity" label="Locked Change" width="120" align="right" />
        <el-table-column prop="balanceQty" label="Balance" width="100" align="right" />
        <el-table-column prop="refNo" label="Ref. Order No." width="180" show-overflow-tooltip />
        <el-table-column prop="operatorName" label="Operator" width="100" />
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
  name: 'InventoryTransactions',
  data() {
    return {
      loading: false,
      list: [],
      total: 0,
      warehouses: [],
      dateRange: [],
      query: { page: 1, pageSize: 20, keyword: '', type: undefined, warehouseId: undefined },
      typeOptions: [
        { label: 'Inbound', value: 1 },
        { label: 'Outbound', value: 2 },
        { label: 'Transfer In', value: 3 },
        { label: 'Transfer Out', value: 4 },
        { label: 'Lock', value: 5 },
        { label: 'Unlock', value: 6 },
        { label: 'Stocktake Adjustment', value: 7 },
        { label: 'Opening Import', value: 8 }
      ]
    }
  },
  created() {
    this.loadWarehouses()
    this.loadData()
  },
  methods: {
    typeName(type) {
      return { 1: 'Inbound', 2: 'Outbound', 3: 'Transfer In', 4: 'Transfer Out', 5: 'Lock', 6: 'Unlock', 7: 'Stocktake Adjustment', 8: 'Opening Import' }[type] || 'Other'
    },
    typeTagType(type) {
      return { 1: 'success', 2: 'danger', 3: 'success', 4: 'warning', 5: 'warning', 6: 'info', 7: 'warning', 8: 'info' }[type] || 'info'
    },
    formatDateTime(value) {
      return value ? value.replace('T', ' ').substring(0, 19) : '—'
    },
    async loadWarehouses() {
      this.warehouses = await warehouseApi.options()
    },
    async loadData() {
      this.loading = true
      try {
        const params = { ...this.query }
        if (this.dateRange && this.dateRange.length === 2) {
          params.startDate = this.dateRange[0]
          params.endDate = this.dateRange[1]
        }
        const result = await inventoryApi.transactions(params)
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
      this.query = { page: 1, pageSize: 20, keyword: '', type: undefined, warehouseId: undefined }
      this.dateRange = []
      this.loadData()
    }
  }
}
</script>
