<template>
  <div class="home-view">
    <!-- 搜索栏 -->
    <el-card class="search-card">
      <el-form :inline="true">
        <el-form-item>
          <el-input 
            v-model="keyword" 
            placeholder="搜索视频..." 
            clearable 
            @keyup.enter="handleSearch"
            style="width: 300px"
          />
        </el-form-item>
        <el-form-item>
          <el-button type="primary" @click="handleSearch">🔍 搜索</el-button>
          <el-button @click="resetSearch">重置</el-button>
        </el-form-item>
        <el-form-item style="float: right;">
          <el-button type="success" @click="showNewTaskDialog = true">➕ 新建爬取任务</el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 视频列表 -->
    <el-card class="video-list-card">
      <template #header>
        <div class="card-header">
          <span>📺 视频列表</span>
          <el-tag type="info">共 {{ total }} 个视频</el-tag>
        </div>
      </template>

      <div v-loading="loading">
        <el-row v-if="videos.length > 0" :gutter="20">
          <el-col 
            :span="6" 
            :xs="24" 
            :sm="12" 
            :md="8" 
            :lg="6"
            v-for="video in videos" 
            :key="video.id"
          >
            <el-card class="video-card" shadow="hover">
              <div class="video-cover">
                <el-image 
                  :src="video.coverImageLocal || video.coverImage || '/placeholder.png'"
                  fit="cover"
                  class="cover-image"
                  lazy
                >
                  <template #placeholder>
                    <div class="image-placeholder">🎬</div>
                  </template>
                  <template #error>
                    <div class="image-placeholder">📺</div>
                  </template>
                </el-image>
                <el-tag v-if="video.isCached" type="success" class="cache-tag">✅ 已缓存</el-tag>
              </div>
              <div class="video-info">
                <h3 class="video-title" :title="video.title">{{ video.title }}</h3>
                <p class="video-meta">
                  <span v-if="video.category">📁 {{ video.category }}</span>
                  <span v-if="video.publishYear">📅 {{ video.publishYear }}</span>
                </p>
                <div class="video-actions">
                  <el-button size="small" type="primary" @click="viewDetail(video.id)">详情</el-button>
                  <el-button 
                    v-if="!video.isCached" 
                    size="small" 
                    type="success" 
                    @click="cacheVideo(video)"
                  >
                    缓存
                  </el-button>
                </div>
              </div>
            </el-card>
          </el-col>
        </el-row>

        <el-empty v-else description="暂无视频数据" />

        <!-- 分页 -->
        <el-pagination
          v-if="total > 0"
          v-model:current-page="currentPage"
          v-model:page-size="pageSize"
          :total="total"
          :page-sizes="[12, 24, 48, 96]"
          layout="total, sizes, prev, pager, next, jumper"
          @current-change="handlePageChange"
          @size-change="handleSizeChange"
          class="pagination"
        />
      </div>
    </el-card>

    <!-- 新建任务对话框 -->
    <el-dialog v-model="showNewTaskDialog" title="新建爬取任务" width="500px">
      <el-form :model="newTask" label-width="100px">
        <el-form-item label="目标 URL" required>
          <el-input 
            v-model="newTask.targetUrl" 
            placeholder="请输入视频列表页 URL"
            rows="2"
            type="textarea"
          />
        </el-form-item>
        <el-form-item label="任务类型" required>
          <el-select v-model="newTask.taskType" style="width: 100%;">
            <el-option label="增量爬取" value="Incremental" />
            <el-option label="全量爬取" value="Full" />
            <el-option label="单个视频" value="Single" />
          </el-select>
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="showNewTaskDialog = false">取消</el-button>
        <el-button type="primary" @click="handleCreateTask" :loading="creating">创建任务</el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { ElMessage } from 'element-plus'
import { useVideoStore, useTaskStore } from '@/stores'
import type { Video } from '@/types'

const videoStore = useVideoStore()
const taskStore = useTaskStore()

const keyword = ref('')
const currentPage = ref(1)
const pageSize = ref(24)
const showNewTaskDialog = ref(false)
const creating = ref(false)
const newTask = ref({
  targetUrl: 'https://b.huaduzy.cc/vodshow/tangxinVlog-----------.html',
  taskType: 'Incremental'
})

const videos = ref<Video[]>([])
const total = ref(0)
const loading = ref(false)

const loadVideos = async () => {
  loading.value = true
  try {
    await videoStore.fetchVideos(currentPage.value, pageSize.value)
    videos.value = videoStore.videos
    total.value = videoStore.total
  } catch (error) {
    ElMessage.error('加载视频失败')
  } finally {
    loading.value = false
  }
}

const handleSearch = async () => {
  if (keyword.value.trim()) {
    loading.value = true
    try {
      await videoStore.searchVideos(keyword.value.trim(), currentPage.value, pageSize.value)
      videos.value = videoStore.videos
      total.value = videoStore.total
    } catch (error) {
      ElMessage.error('搜索失败')
    } finally {
      loading.value = false
    }
  } else {
    loadVideos()
  }
}

const resetSearch = () => {
  keyword.value = ''
  loadVideos()
}

const handlePageChange = (page: number) => {
  currentPage.value = page
  if (keyword.value.trim()) {
    handleSearch()
  } else {
    loadVideos()
  }
}

const handleSizeChange = (size: number) => {
  pageSize.value = size
  currentPage.value = 1
  handlePageChange(1)
}

const viewDetail = (id: string) => {
  ElMessage.info('详情页开发中...')
}

const cacheVideo = async (video: Video) => {
  ElMessage.info('缓存功能开发中...')
}

const handleCreateTask = async () => {
  if (!newTask.value.targetUrl.trim()) {
    ElMessage.warning('请输入目标 URL')
    return
  }

  creating.value = true
  try {
    await taskStore.createTask(newTask.value.targetUrl, newTask.value.taskType)
    ElMessage.success('任务创建成功！')
    showNewTaskDialog.value = false
    // 跳转到任务页面
  } catch (error) {
    ElMessage.error('创建任务失败')
  } finally {
    creating.value = false
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
  transition: transform 0.2s;
}

.video-card:hover {
  transform: translateY(-4px);
}

.video-cover {
  position: relative;
  height: 200px;
  overflow: hidden;
  background: #f5f7fa;
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
  background: #f5f7fa;
}

.cache-tag {
  position: absolute;
  top: 10px;
  right: 10px;
}

.video-info {
  padding: 12px;
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
  line-height: 1.4;
}

.video-meta {
  font-size: 12px;
  color: #909399;
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
