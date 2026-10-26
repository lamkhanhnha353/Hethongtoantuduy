import { useState } from 'react';
import { Text, TouchableOpacity, View } from 'react-native';
import { Feather } from '@expo/vector-icons';

import type { Invoice, InvoiceStatus } from '@/types/invoice';

interface InvoiceCardProps {
  invoice: Invoice;
  isProcessing: boolean;
  onCancel: (invoiceId: number) => void;
  onPay: (invoiceId: number) => void;
  onOpenCourse: (courseLevelId: number) => void;
}

const statusDisplay: Record<InvoiceStatus, { label: string; color: string; background: string; icon: string }> = {
  PAID: { label: 'ĐÃ THANH TOÁN', color: '#047857', background: '#d1fae5', icon: 'check-circle' },
  PENDING: { label: 'CHỜ THANH TOÁN', color: '#b45309', background: '#fef3c7', icon: 'clock' },
  FAILED: { label: 'THANH TOÁN THẤT BẠI', color: '#be123c', background: '#ffe4e6', icon: 'alert-circle' },
  CANCELLED: { label: 'ĐÃ HỦY', color: '#64748b', background: '#f1f5f9', icon: 'x-circle' },
  REFUNDED: { label: 'ĐÃ HOÀN TIỀN', color: '#475569', background: '#e2e8f0', icon: 'rotate-ccw' },
};

export function InvoiceCard({ invoice, isProcessing, onCancel, onPay, onOpenCourse }: InvoiceCardProps) {
  const [isExpanded, setIsExpanded] = useState(false);
  const status = statusDisplay[invoice.status] ?? statusDisplay.FAILED;
  const canPay = invoice.status === 'PENDING' || invoice.status === 'FAILED';

  return (
    <View className="mb-3 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm">
      <View className="mb-3 flex-row items-center justify-between border-b border-slate-100 pb-3">
        <Text className="text-[12px] font-semibold text-slate-500">Hóa đơn #{invoice.invoiceId}</Text>
        <View className="flex-row items-center gap-1.5 rounded-full px-2.5 py-1" style={{ backgroundColor: status.background }}>
          <Feather name={status.icon as never} size={12} color={status.color} />
          <Text className="text-[10px] font-bold" style={{ color: status.color }}>{status.label}</Text>
        </View>
      </View>

      <Text className="mb-1 text-[16px] font-bold text-slate-800">
        {invoice.courseLevelName || invoice.className}
      </Text>
      <Text className="mb-3 text-[13px] text-slate-600">Lớp: <Text className="font-semibold text-slate-800">{invoice.className}</Text></Text>

      <View className="mb-4 flex-row flex-wrap gap-x-4 gap-y-2">
        <View className="flex-row items-center gap-1.5">
          <Feather name="user" size={14} color="#64748b" />
          <Text className="text-[12px] text-slate-600">{invoice.studentName}</Text>
        </View>
        <View className="flex-row items-center gap-1.5">
          <Feather name="calendar" size={14} color="#64748b" />
          <Text className="text-[12px] text-slate-600">
            {new Date(invoice.createdAt).toLocaleDateString('vi-VN')}
          </Text>
        </View>
      </View>

      {invoice.status === 'PENDING' && (
        <View className="mb-3 rounded-xl border border-amber-200 bg-amber-50 p-3">
          <Text className="text-[12px] font-medium leading-5 text-amber-800">
            Bé đã được giữ chỗ tại lớp này. Hóa đơn đang chờ thanh toán.
          </Text>
        </View>
      )}
      {invoice.status === 'FAILED' && (
        <View className="mb-3 rounded-xl border border-rose-200 bg-rose-50 p-3">
          <Text className="text-[12px] font-medium leading-5 text-rose-800">
            Thanh toán lần trước chưa hoàn tất. Bạn có thể thử thanh toán lại nếu chỗ của bé vẫn đang được giữ.
          </Text>
        </View>
      )}

      {isExpanded && (
        <View className="mb-3 rounded-xl bg-slate-50 p-3">
          <Text className="mb-1 text-[12px] text-slate-600">Mã giao dịch: {invoice.txnRef || '—'}</Text>
          <Text className="mb-1 text-[12px] text-slate-600">
            Phương thức: {invoice.paymentMethod || 'VNPay'}
          </Text>
          {invoice.paidAt && (
            <Text className="text-[12px] text-slate-600">
              Ngày thanh toán: {new Date(invoice.paidAt).toLocaleString('vi-VN')}
            </Text>
          )}
        </View>
      )}

      <TouchableOpacity
        activeOpacity={0.7}
        className="mb-3 flex-row items-center justify-center gap-2 rounded-xl border border-indigo-100 bg-indigo-50 py-2.5"
        onPress={() => onOpenCourse(invoice.courseLevelId)}
        disabled={!Number.isInteger(invoice.courseLevelId) || invoice.courseLevelId <= 0}
      >
        <Feather name="book-open" size={15} color="#4f46e5" />
        <Text className="text-[13px] font-bold text-indigo-700">Mở khóa học</Text>
        <Feather name="arrow-up-right" size={14} color="#4f46e5" />
      </TouchableOpacity>

      <View className="flex-row items-end justify-between border-t border-slate-100 pt-3">
        <View>
          <Text className="mb-0.5 text-[11px] text-slate-500">Tổng tiền</Text>
          <Text className="text-[16px] font-black text-indigo-600">
            {invoice.amount.toLocaleString('vi-VN')}đ
          </Text>
          <TouchableOpacity onPress={() => setIsExpanded(value => !value)} className="mt-1">
            <Text className="text-[11px] font-semibold text-slate-500">
              {isExpanded ? 'Ẩn chi tiết' : 'Chi tiết hóa đơn'}
            </Text>
          </TouchableOpacity>
        </View>

        <View className="flex-row gap-2">
          {invoice.status === 'PENDING' && (
            <TouchableOpacity
              disabled={isProcessing}
              onPress={() => onCancel(invoice.invoiceId)}
              className="justify-center rounded-xl border border-slate-200 bg-slate-100 px-3 py-2"
            >
              <Text className="text-[12px] font-bold text-slate-600">Hủy</Text>
            </TouchableOpacity>
          )}
          {canPay && (
            <TouchableOpacity
              disabled={isProcessing}
              onPress={() => onPay(invoice.invoiceId)}
              className="flex-row items-center gap-1.5 rounded-xl bg-indigo-600 px-3 py-2"
            >
              <Text className="text-[12px] font-bold text-white">
                {invoice.status === 'FAILED' ? 'Thanh toán lại' : 'Thanh toán'}
              </Text>
              <Feather name="arrow-right" size={13} color="white" />
            </TouchableOpacity>
          )}
        </View>
      </View>
    </View>
  );
}
