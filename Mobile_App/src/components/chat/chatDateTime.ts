const VIETNAM_TIME_ZONE = 'Asia/Ho_Chi_Minh';

function parseChatDate(value: string) {
  const hasTimeZone = /(?:Z|[+-]\d{2}:?\d{2})$/i.test(value);
  return new Date(hasTimeZone ? value : `${value}Z`);
}

export function getChatTimestamp(value: string) {
  return parseChatDate(value).getTime();
}

export function formatChatTime(value: string) {
  return parseChatDate(value).toLocaleTimeString('vi-VN', {
    hour: '2-digit',
    minute: '2-digit',
    timeZone: VIETNAM_TIME_ZONE,
  });
}

export function formatChatDate(value: string) {
  return parseChatDate(value).toLocaleDateString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    timeZone: VIETNAM_TIME_ZONE,
  });
}
