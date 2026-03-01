<template>
  <div class="home-view">
    <el-card class="search-card">
      <el-form :inline="true">
        <el-form-item>
          <el-input v-model="keyword" placeholder="搜索视频..." clearable @keyup.enter="search" />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="search">🔍 搜索</el-button>
        </el-form-item>
        <el-form-item>
          <el-button type="success" @click="showNewTaskDialog = true">➕ 新建爬取任务</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <el-card class="video-list-card">
      <template #header>
        <div class="card-header">
          <span>📺 视频列表</span>
          <el-tag type="info">共 {{ total }} 个视频</el-tag>
        </div>
      </template>

      <el-row :gutter="20">
        <el-col :span="6" v-for="video in videos" :key="video.id">
          <el-card class="video-card" shadow="hover">
            <div class="video-cover">
              <el-image 
                :src="video.coverImageLocal || video.coverImage || '/placeholder.png'"
                fit="cover"
                class="cover-image"
              >
                <template #placeholder>
                  <div class="image-placeholder">🎬</div>
                </template>
              </el-image>
              <el-tag v-if="video.isCached" type="success" class="cache-tag">✅ 已缓存</el-tag>
            </div>
            <div class="video-info">
              <h3 class="video-title">{{ video.title }}</h3>
              <p class="video-meta">
                <span v-if="video.category">📁 {{ video.category }}</span>
                <span v-if="video.publishYear">📅 {{ video.publishYear }}</span>
              </p>
              <div class="video-actions">
                <el-button size="small" type="primary" @click="viewDetail(video.id)">详情</el-button>
                <el-button v-if="!video.isCached" size="small" type="success">缓存</el-button>
              </div>
            </div>
          </el-card>
        </el-col>
      </el-row>

      <el-pagination
        v-model:current-page="page"
        v-model:page-size="pageSize"
        :total="total"
        :page-sizes="[12, 24, 48, 96]"
        layout="total, sizes, prev, pager, next"
        @current-change="loadVideos"
        @size-change="loadVideos"
        class="pagination"
      />
    </el-card>

    <!-- 新建任务对话框 -->
    <el-dialog v-model="showNewTaskDialog" title="新建爬取任务" width="500px">
      <el-form :model="newTask" label-width="100px">
        <el-form-item label="目标 URL">
          <el-input v-model="newTask.targetUrl" placeholder="请输入视频列表页 URL" />
        </el-form-item>
        <el-form-item label="任务类型">
          <el-select v-model="newTask.taskType">
            <el-option label="增量爬取" value="Incremental" />
            <el-option label="全量爬取" value="Full" />
            <el-option label="单个视频" value="Single" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showNewTaskDialog = false">取消</el-button>
        <el-button type="primary" @click="createTask">创建任务</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import axios from 'axios'

const videos = ref<any[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(24)
const keyword = ref('')
const showNewTaskDialog = ref(false)
const newTask = ref({
  targetUrl: 'https://b.huaduzy.cc/vodshow/tangxinVlog-----------.html',
  taskType: 'Incremental'
})

const loadVideos = async () => {
  try {
    const response = await axios.get(`/api/videos?page=${page.value}&pageSize=${pageSize.value}`)
    videos.value = response.data.items || []
    total.value = response.data.total || 0
  } catch (error) {
    console.error('加载视频失败:', error)
  }
}

const search = async () => {
  if (keyword.value) {
    const response = await axios.get(`/api/videos/search?keyword=${keyword.value}&page=${page.value}&pageSize=${pageSize.value}`)
    videos.value = response.data.items || []
    total.value = response.data.total || 0
  } else {
    loadVideos()
  }
}

const viewDetail = (id: string) => {
  // TODO: 跳转到详情页
  console.log('查看视频详情:', id)
}

const createTask = async () => {
  try {
    await axios.post('/api/crawlerTasks', newTask.value)
    showNewTaskDialog.value = false
    alert('任务创建成功！')
  } catch (error) {
    alert('创建任务失败')
  }
}

onMounted(() => {
  loadVideos()
})
</script>

<style scoped>
.home-view {
  max-width: 1400px;
  margin: 0 auto;
}

.search-card {
  margin-bottom: 20px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.video-list-card {
  min-height: 600px;
}

.video-card {
  margin-bottom: 20px;
}

.video-cover {
  position: relative;
  height: 200px;
  overflow: hidden;
}

.cover-image {
  width: 100%;
  height: 100%;
}

.image-placeholder {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  font-size: 48px;
  background: #f0f0f0;
}

.cache-tag {
  position: absolute;
  top: 10px;
  right: 10px;
}

.video-info {
  padding: 10px;
}

.video-title {
  font-size: 14px;
  margin: 0 0 10px 0;
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  height: 40px;
}

.video-meta {
  font-size: 12px;
  color: #999;
  margin: 0 0 10px 0;
  display: flex;
  gap: 10px;
}

.video-actions {
  display: flex;
  gap: 5px;
}

.pagination {
  margin-top: 20px;
  justify-content: center;
}
</style>
