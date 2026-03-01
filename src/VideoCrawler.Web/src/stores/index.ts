import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import type { Video, CrawlerTask, PagedResult } from '@/types'
import { videoApi, taskApi } from '@/api'

export const useVideoStore = defineStore('video', () => {
  const videos = ref<Video[]>([])
  const total = ref(0)
  const currentPage = ref(1)
  const pageSize = ref(24)
  const loading = ref(false)

  const totalPages = computed(() => Math.ceil(total.value / pageSize.value))

  async function fetchVideos(page = 1, size = 24) {
    loading.value = true
    try {
      const result: PagedResult<Video> = await videoApi.getList({ page, pageSize: size })
      videos.value = result.items
      total.value = result.total
      currentPage.value = page
      pageSize.value = size
    } catch (error) {
      console.error('获取视频列表失败:', error)
    } finally {
      loading.value = false
    }
  }

  async function searchVideos(keyword: string, page = 1, size = 24) {
    loading.value = true
    try {
      const result: PagedResult<Video> = await videoApi.search(keyword, { page, pageSize: size })
      videos.value = result.items
      total.value = result.total
      currentPage.value = page
      pageSize.value = size
    } catch (error) {
      console.error('搜索视频失败:', error)
    } finally {
      loading.value = false
    }
  }

  async function fetchCachedVideos(page = 1, size = 24) {
    loading.value = true
    try {
      const result: PagedResult<Video> = await videoApi.getCached({ page, pageSize: size })
      videos.value = result.items
      total.value = result.total
      currentPage.value = page
      pageSize.value = size
    } catch (error) {
      console.error('获取已缓存视频失败:', error)
    } finally {
      loading.value = false
    }
  }

  return {
    videos,
    total,
    currentPage,
    pageSize,
    totalPages,
    loading,
    fetchVideos,
    searchVideos,
    fetchCachedVideos
  }
})

export const useTaskStore = defineStore('task', () => {
  const tasks = ref<CrawlerTask[]>([])
  const loading = ref(false)

  async function fetchTasks(status?: string, limit = 50) {
    loading.value = true
    try {
      tasks.value = await taskApi.getList({ status, limit })
    } catch (error) {
      console.error('获取任务列表失败:', error)
    } finally {
      loading.value = false
    }
  }

  async function createTask(targetUrl: string, taskType = 'Incremental') {
    return await taskApi.create({ targetUrl, taskType })
  }

  async function startTask(id: string) {
    return await taskApi.start(id)
  }

  async function cancelTask(id: string) {
    return await taskApi.cancel(id)
  }

  async function retryTask(id: string) {
    return await taskApi.retry(id)
  }

  return {
    tasks,
    loading,
    fetchTasks,
    createTask,
    startTask,
    cancelTask,
    retryTask
  }
})
