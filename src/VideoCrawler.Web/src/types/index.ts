export interface Video {
  id: string
  title: string
  description?: string
  coverImage?: string
  coverImageLocal?: string
  videoUrl: string
  videoUrlLocal?: string
  m3u8Url?: string
  category?: string
  tags?: string
  actor?: string
  director?: string
  duration?: number
  publishYear?: number
  area?: string
  language?: string
  rating?: number
  episodeCount?: number
  currentEpisode?: string
  status?: string
  sourceUrl: string
  sourceSite: string
  crawlTime?: string
  lastUpdateTime?: string
  isCached: boolean
  cachePath?: string
  createdAt: string
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
  totalPages: number
}

export interface CrawlerTask {
  id: string
  taskName: string
  targetUrl: string
  taskType: string
  status: string
  totalCount: number
  processedCount: number
  successCount: number
  failedCount: number
  errorMessage?: string
  startTime?: string
  endTime?: string
  assignedWorker?: string
  priority: number
  retryCount: number
  progress: number
}

export interface Worker {
  workerId: string
  workerName: string
  status: string
  currentTaskId?: string
  completedTasks: number
  failedTasks: number
  lastHeartbeat: string
}

export interface SiteAnalysis {
  url: string
  analyzedAt: string
  success: boolean
  error?: string
  htmlLength: number
  title: string
  jsonLdData: string[]
  jsonLdTypes: string[]
  videoListSelectors: Record<string, number>
  detailSelectors: Record<string, number>
  m3u8Urls: string[]
  totalLinks: number
  videoLinks: Array<{
    url: string
    text: string
    title: string
  }>
  totalImages: number
  coverImages: Array<{
    src: string
    alt: string
    class: string
  }>
  metaData: Record<string, string>
  recommendedParser: string
}
