<template>
  <div class="tasks-view">
    <el-card>
      <template #header>
        <div class="card-header">
          <span>📋 爬取任务</span>
          <el-button type="primary" @click="createTask">➕ 新建任务</el-button>
        </div>
      </template>

      <el-table :data="tasks" stripe>
        <el-table-column prop="taskName" label="任务名称" />
        <el-table-column prop="targetUrl" label="目标 URL" min-width="200" show-overflow-tooltip />
        <el-table-column prop="taskType" label="类型" width="100">
          <template #default="{ row }">
            <el-tag :type="row.taskType === 'Full' ? 'warning' : 'info'">
              {{ row.taskType === 'Full' ? '全量' : '增量' }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column prop="status" label="状态" width="100">
          <template #default="{ row }">
            <el-tag :type="getStatusType(row.status)">
              {{ getStatusText(row.status) }}
            </el-tag>
          </template>
        </el-table-column>
        <el-table-column label="进度" width="150">
          <template #default="{ row }">
            <el-progress 
              :percentage="row.progress" 
              :status="row.status === 'Failed' ? 'exception' : row.status === 'Completed' ? 'success' : undefined"
            />
          </template>
        </el-table-column>
        <el-table-column prop="successCount" label="成功" width="80" />
        <el-table-column prop="failedCount" label="失败" width="80" />
        <el-table-column prop="startTime" label="开始时间" width="180" />
        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-button 
              v-if="row.status === 'Pending'" 
              size="small" 
              type="success" 
              @click="startTask(row.id)"
            >
              启动
            </el-button>
            <el-button 
              v-if="row.status === 'Running'" 
              size="small" 
              type="warning" 
              @click="cancelTask(row.id)"
            >
              取消
            </el-button>
            <el-button 
              v-if="row.status === 'Failed'" 
              size="small" 
              type="info" 
              @click="retryTask(row.id)"
            >
              重试
            </el-button>
            <el-button size="small" @click="viewDetail(row.id)">详情</el-button>
          </template>
        </el-table-column>
      </el-table>
    </el-card>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import axios from 'axios'

const tasks = ref<any[]>([])

const getStatusType = (status: string) => {
  const map: Record<string, any> = {
    'Pending': 'info',
    'Running': 'warning',
    'Completed': 'success',
    'Failed': 'danger',
    'Cancelled': 'info'
  }
  return map[status] || 'info'
}

const getStatusText = (status: string) => {
  const map: Record<string, string> = {
    'Pending': '等待中',
    'Running': '运行中',
    'Completed': '已完成',
    'Failed': '失败',
    'Cancelled': '已取消'
  }
  return map[status] || status
}

const loadTasks = async () => {
  try {
    const response = await axios.get('/api/crawlerTasks')
    tasks.value = response.data || []
  } catch (error) {
    console.error('加载任务失败:', error)
  }
}

const createTask = async () => {
  const url = prompt('请输入目标 URL:', 'https://b.huaduzy.cc/vodshow/tangxinVlog-----------.html')
  if (!url) return

  try {
    await axios.post('/api/crawlerTasks', { targetUrl: url, taskType: 'Incremental' })
    await loadTasks()
    alert('任务创建成功！')
  } catch (error) {
    alert('创建任务失败')
  }
}

const startTask = async (id: string) => {
  try {
    await axios.post(`/api/crawlerTasks/${id}/start`)
    await loadTasks()
    alert('任务已启动')
  } catch (error) {
    alert('启动失败')
  }
}

const cancelTask = async (id: string) => {
  try {
    await axios.post(`/api/crawlerTasks/${id}/cancel`)
    await loadTasks()
    alert('任务已取消')
  } catch (error) {
    alert('取消失败')
  }
}

const retryTask = async (id: string) => {
  try {
    await axios.post(`/api/crawlerTasks/${id}/retry`)
    await loadTasks()
    alert('任务已重新排队')
  } catch (error) {
    alert('重试失败')
  }
}

const viewDetail = (id: string) => {
  console.log('查看任务详情:', id)
}

onMounted(() => {
  loadTasks()
})
</script>

<style scoped>
.tasks-view {
  max-width: 1400px;
  margin: 0 auto;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}
</style>
