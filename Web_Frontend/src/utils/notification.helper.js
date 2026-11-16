import {
  BellAlertIcon,
  InformationCircleIcon,
  ClipboardDocumentCheckIcon,
  DocumentTextIcon,
  AcademicCapIcon,
  CurrencyDollarIcon,
  UserPlusIcon,
  ShieldCheckIcon,
  MegaphoneIcon,
} from "@heroicons/vue/24/outline";

const NOTIFICATION_TYPE_META = {
  ANNOUNCEMENT: {
    label: "Thông báo chung",
    icon: MegaphoneIcon,
    iconBg: "bg-orange-600",
    badgeClass: "border-orange-300 bg-orange-50 text-orange-800",
    urgent: false,
  },
  SPECIAL: {
    label: "Thông báo đặc biệt",
    icon: BellAlertIcon,
    iconBg: "bg-rose-600",
    badgeClass: "border-rose-300 bg-rose-50 text-rose-800",
    urgent: true,
  },
  SYSTEM: {
    label: "Hệ thống",
    icon: InformationCircleIcon,
    iconBg: "bg-stone-700",
    badgeClass: "border-stone-300 bg-stone-100 text-stone-700",
    urgent: false,
  },
  ATTENDANCE: {
    label: "Điểm danh",
    icon: ClipboardDocumentCheckIcon,
    iconBg: "bg-emerald-600",
    badgeClass: "border-emerald-300 bg-emerald-50 text-emerald-800",
    urgent: false,
  },
  LEAVE_REQUEST: {
    label: "Đơn xin nghỉ",
    icon: DocumentTextIcon,
    iconBg: "bg-amber-600",
    badgeClass: "border-amber-300 bg-amber-50 text-amber-800",
    urgent: true,
  },
  ACADEMIC: {
    label: "Học tập",
    icon: AcademicCapIcon,
    iconBg: "bg-orange-600",
    badgeClass: "border-orange-300 bg-orange-50 text-orange-800",
    urgent: false,
  },
  TUITION: {
    label: "Học phí",
    icon: CurrencyDollarIcon,
    iconBg: "bg-emerald-600",
    badgeClass: "border-emerald-300 bg-emerald-50 text-emerald-800",
    urgent: false,
  },
  PAYMENT: {
    label: "Thanh toán",
    icon: CurrencyDollarIcon,
    iconBg: "bg-emerald-600",
    badgeClass: "border-emerald-300 bg-emerald-50 text-emerald-800",
    urgent: false,
  },
  ENROLLMENT: {
    label: "Ghi danh",
    icon: UserPlusIcon,
    iconBg: "bg-sky-600",
    badgeClass: "border-sky-300 bg-sky-50 text-sky-800",
    urgent: false,
  },
  ACCOUNT: {
    label: "Tài khoản",
    icon: ShieldCheckIcon,
    iconBg: "bg-stone-700",
    badgeClass: "border-stone-300 bg-stone-100 text-stone-700",
    urgent: false,
  },
};

const DEFAULT_TYPE_META = {
  label: "Thông báo",
  icon: BellAlertIcon,
  iconBg: "bg-orange-600",
  badgeClass: "border-orange-300 bg-orange-50 text-orange-800",
  urgent: false,
};

export const NOTIFICATION_TYPE_OPTIONS = [
  { value: "", label: "Tất cả loại" },
  { value: "SPECIAL", label: "Thông báo đặc biệt" },
  { value: "ANNOUNCEMENT", label: "Thông báo chung" },
  { value: "SYSTEM", label: "Hệ thống" },
  { value: "ATTENDANCE", label: "Điểm danh" },
  { value: "LEAVE_REQUEST", label: "Đơn xin nghỉ" },
  { value: "ACADEMIC", label: "Học tập" },
  { value: "TUITION", label: "Học phí" },
  { value: "PAYMENT", label: "Thanh toán" },
  { value: "ENROLLMENT", label: "Ghi danh" },
  { value: "ACCOUNT", label: "Tài khoản" },
];

export function getNotificationTypeMeta(type) {
  const key = String(type || "").trim().toUpperCase();
  return NOTIFICATION_TYPE_META[key] || DEFAULT_TYPE_META;
}

export function formatRelativeNotificationTime(value) {
  if (!value) return "—";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return String(value);

  const diffSeconds = Math.floor((Date.now() - date.getTime()) / 1000);
  if (diffSeconds < 60) return "Vừa xong";

  const diffMinutes = Math.floor(diffSeconds / 60);
  if (diffMinutes < 60) return `${diffMinutes} phút trước`;

  const diffHours = Math.floor(diffMinutes / 60);
  if (diffHours < 24) return `${diffHours} giờ trước`;

  const diffDays = Math.floor(diffHours / 24);
  if (diffDays < 7) return `${diffDays} ngày trước`;

  return date.toLocaleString("vi-VN", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  });
}

export function formatFullNotificationTime(value) {
  if (!value) return "—";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return String(value);

  return date.toLocaleString("vi-VN", {
    weekday: "long",
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    hour12: false,
  });
}

/**
 * Returns the contextual route for a notification if one applies, or null if the
 * notification is purely informational (inbox detail is sufficient).
 */
export function resolveNotificationActionRoute(notif, role) {
  const normalizedRole = String(role || "").trim().toLowerCase();
  const type = String(notif?.type || "").trim().toUpperCase();

  if (normalizedRole === "teacher") {
    switch (type) {
      case "LEAVE_REQUEST":
      case "ATTENDANCE":
        return { path: "/teacher/attendance", label: "Đi tới điểm danh" };
      case "ACADEMIC":
        return { path: "/teacher/grading", label: "Đi tới chấm điểm" };
      case "ENROLLMENT":
        return { path: "/teacher/students", label: "Xem học sinh" };
      case "ACCOUNT":
        return { path: "/teacher/account/profile", label: "Hồ sơ tài khoản" };
      default:
        return null;
    }
  }

  switch (type) {
    case "LEAVE_REQUEST":
    case "ATTENDANCE":
      return { path: "/reports/attendance", label: "Xem chuyên cần" };
    case "PAYMENT":
    case "TUITION":
      return { path: "/finance/invoices", label: "Xem hóa đơn" };
    case "ENROLLMENT":
      return { path: "/training/classes", label: "Xem lớp học" };
    case "ACCOUNT":
      return { path: "/account/profile", label: "Hồ sơ tài khoản" };
    default:
      return null;
  }
}
