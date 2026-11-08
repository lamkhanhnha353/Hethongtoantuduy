/**
 * Utility helper for Media (Images, YouTube Videos) and Score Calculation
 */

// ─── YouTube URL Detection & Embed ───────────────────────────────────────────

/**
 * Extracts YouTube video ID from various YouTube URL formats:
 * - https://www.youtube.com/watch?v=VIDEO_ID
 * - https://youtu.be/VIDEO_ID
 * - https://www.youtube.com/embed/VIDEO_ID
 * - https://www.youtube.com/shorts/VIDEO_ID
 * - https://m.youtube.com/watch?v=VIDEO_ID
 */
export function getYouTubeVideoId(url) {
  if (!url || typeof url !== 'string') return null
  const trimmed = url.trim()
  const regExp = /^.*(youtu.be\/|v\/|u\/\w\/|embed\/|shorts\/|watch\?v=|&v=)([^#&?]*).*/
  const match = trimmed.match(regExp)
  return match && match[2].length === 11 ? match[2] : null
}

/**
 * Checks if a URL is a YouTube video URL
 */
export function isYouTubeUrl(url) {
  return !!getYouTubeVideoId(url)
}

/**
 * Converts any YouTube URL to an embed URL suitable for <iframe>
 */
export function getYouTubeEmbedUrl(url) {
  const videoId = getYouTubeVideoId(url)
  return videoId ? `https://www.youtube.com/embed/${videoId}` : null
}

/**
 * Checks if a string is an image URL or data URL
 */
export function isImageUrl(url) {
  if (!url || typeof url !== 'string') return false
  const trimmed = url.trim()
  if (trimmed.startsWith('data:image/')) return true
  if (isYouTubeUrl(trimmed)) return false
  return /\.(jpeg|jpg|gif|png|webp|svg)($|\?)/i.test(trimmed) || trimmed.startsWith('http')
}

/**
 * Reads a browser File object as Base64 Data URL
 * @param {File} file
 * @returns {Promise<string>}
 */
export function fileToBase64(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader()
    reader.onload = () => resolve(reader.result)
    reader.onerror = (err) => reject(err)
    reader.readAsDataURL(file)
  })
}

// ─── Score & Difficulty Constraints ──────────────────────────────────────────

/**
 * Ràng buộc điểm theo mốc chuẩn:
 * - Dễ (EASY): 1.0 điểm
 * - Trung bình (MEDIUM): 1.5 điểm
 * - Khó (HARD): 2.0 điểm
 */
export const DIFFICULTY_POINTS = {
  EASY: 1.0,
  MEDIUM: 1.5,
  HARD: 2.0,
}

export function getDifficultyPoint(difficulty) {
  const d = (difficulty || '').toUpperCase()
  return DIFFICULTY_POINTS[d] !== undefined ? DIFFICULTY_POINTS[d] : 1.0
}

/**
 * Cách tính điểm bài kiểm tra:
 * Điểm = (Tổng điểm đạt / Tổng số điểm) * 100
 * Thang điểm 100
 * @param {number} earnedPoints - Tổng số điểm đạt được
 * @param {number} totalPoints - Tổng số điểm của bài kiểm tra
 * @returns {number} Điểm quy đổi thang 100 (làm tròn 1 chữ số thập phân)
 */
export function calculateExamScore(earnedPoints, totalPoints) {
  const earned = parseFloat(earnedPoints) || 0
  const total = parseFloat(totalPoints) || 0
  if (total <= 0) return 0
  const rawScore = (earned / total) * 100
  return Math.min(100, Math.max(0, Math.round(rawScore * 10) / 10))
}

export function normalizeScore(value) {
  if (value === null || value === undefined || value === '') return null
  const score = Number(value)
  if (!Number.isFinite(score)) return null
  return Math.min(100, Math.max(0, Math.round(score * 10) / 10))
}

// ─── Multiple Choice Helper ──────────────────────────────────────────────────

/**
 * Parses multiple choice options from a content string if they exist:
 * E.g.
 * "Đề bài...
 * A. Phương án 1
 * B. Phương án 2
 * C. Phương án 3
 * D. Phương án 4"
 */
export function parseMultipleChoice(content = '', answer = '') {
  let questionText = content || ''
  let optionA = ''
  let optionB = ''
  let optionC = ''
  let optionD = ''
  let correctOption = 'A'
  let explanation = ''

  // Try regex for "\nA. ...\nB. ...\nC. ...\nD. ..." or "A. ...; B. ..."
  const mcRegex = /(?:^|\n|\s)A[\.:\)]\s*([\s\S]*?)(?:^|\n|\s)B[\.:\)]\s*([\s\S]*?)(?:^|\n|\s)C[\.:\)]\s*([\s\S]*?)(?:^|\n|\s)D[\.:\)]\s*([\s\S]*?)$/i
  const match = content.match(mcRegex)

  if (match) {
    const aIndex = content.search(/(?:^|\n|\s)A[\.:\)]\s*/i)
    if (aIndex >= 0) {
      questionText = content.substring(0, aIndex).trim()
    }
    optionA = (match[1] || '').trim().replace(/;$/, '')
    optionB = (match[2] || '').trim().replace(/;$/, '')
    optionC = (match[3] || '').trim().replace(/;$/, '')
    optionD = (match[4] || '').trim().replace(/;$/, '')
  }

  // Parse answer
  const ansStr = (answer || '').trim()
  const ansMatch = ansStr.match(/^([ABCD])(?:[\.:\)\s-]|[\s]*)([\s\S]*)$/i)
  if (ansMatch) {
    correctOption = ansMatch[1].toUpperCase()
    explanation = (ansMatch[2] || '').trim()
  } else if (['A', 'B', 'C', 'D'].includes(ansStr.toUpperCase())) {
    correctOption = ansStr.toUpperCase()
  } else {
    explanation = ansStr
  }

  return {
    questionText,
    optionA,
    optionB,
    optionC,
    optionD,
    correctOption,
    explanation,
  }
}

/**
 * Builds standard content string for multiple choice question
 */
export function buildMultipleChoiceContent(questionText, options) {
  const qText = (questionText || '').trim()
  const a = (options.A || '').trim()
  const b = (options.B || '').trim()
  const c = (options.C || '').trim()
  const d = (options.D || '').trim()
  return `${qText}\nA. ${a}\nB. ${b}\nC. ${c}\nD. ${d}`
}

/**
 * Builds standard answer string for multiple choice question
 */
export function buildMultipleChoiceAnswer(correctOption, options, explanation = '') {
  const opt = (correctOption || 'A').toUpperCase()
  const optText = (options[opt] || '').trim()
  const exp = (explanation || '').trim()

  if (exp) {
    return `${opt}. ${optText}\n- Lời giải: ${exp}`
  }
  return `${opt}. ${optText}`
}
