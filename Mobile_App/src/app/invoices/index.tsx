import { useCallback, useMemo, useState } from 'react';
import { ActivityIndicator, Alert, RefreshControl, ScrollView, Text, TouchableOpacity, View } from 'react-native';
import { useFocusEffect, useRouter } from 'expo-router';
import { SafeAreaView } from 'react-native-safe-area-context';
import { Feather, Ionicons } from '@expo/vector-icons';
import * as WebBrowser from 'expo-web-browser';
import * as Linking from 'expo-linking';

import { API_URL } from '@/config/env';
import { useSession } from '@/hooks/useSession';
import { InvoiceCard } from '@/components/invoices/InvoiceCard';
import { invoiceService } from '@/services/invoiceService';
import type { Invoice, InvoiceFilter } from '@/types/invoice';

const filters: { value: InvoiceFilter; label: string }[] = [
  { value: 'ALL', label: 'Tất cả' },
  { value: 'PENDING', label: 'Chờ thanh toán' },
  { value: 'PAID', label: 'Đã thanh toán' },
  { value: 'OTHER', label: 'Khác' },
];

export default function InvoicesScreen() {
  const router = useRouter();
  const { session, isHydrated } = useSession();

  const [invoices, setInvoices] = useState<Invoice[]>([]);
  const [filter, setFilter] = useState<InvoiceFilter>('ALL');
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [processingInvoiceId, setProcessingInvoiceId] = useState<number | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const getAccessToken = useCallback(() => {
    if (typeof session === 'string') {
      try {
        return JSON.parse(session).accessToken || session;
      } catch {
        return session;
      }
    }
    return session && typeof session === 'object' ? (session as any).accessToken : '';
  }, [session]);

  const fetchInvoices = useCallback(async (refresh = false) => {
    if (refresh) setIsRefreshing(true);
    else setIsLoading(true);

    try {
      const token = getAccessToken();
      if (!token) throw new Error('Vui lòng đăng nhập lại để xem hóa đơn.');

      const data = await invoiceService.getHistory(token);
      setInvoices(data);
      setErrorMessage(null);
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : 'Đã xảy ra lỗi khi tải danh sách hóa đơn.');
    } finally {
      setIsLoading(false);
      setIsRefreshing(false);
    }
  }, [getAccessToken]);

  useFocusEffect(
    useCallback(() => {
      if (isHydrated) {
        void fetchInvoices();
      }
    }, [fetchInvoices, isHydrated])
  );

  const filteredInvoices = useMemo(() => invoices.filter(invoice => {
    if (filter === 'PENDING') return invoice.status === 'PENDING';
    if (filter === 'PAID') return invoice.status === 'PAID';
    if (filter === 'OTHER') return invoice.status !== 'PENDING' && invoice.status !== 'PAID';
    return true;
  }), [filter, invoices]);

  const counts = useMemo(() => ({
    all: invoices.length,
    pending: invoices.filter(invoice => invoice.status === 'PENDING').length,
    paid: invoices.filter(invoice => invoice.status === 'PAID').length,
  }), [invoices]);

  const handleCancelInvoice = (invoiceId: number) => {
    Alert.alert(
      'Xác nhận hủy hóa đơn',
      'Bạn có chắc chắn muốn hủy hóa đơn này không? Chỗ đang giữ cho bé cũng sẽ được giải phóng.',
      [
        { text: 'Giữ hóa đơn', style: 'cancel' },
        {
          text: 'Hủy hóa đơn',
          style: 'destructive',
          onPress: async () => {
            setProcessingInvoiceId(invoiceId);
            try {
              await invoiceService.cancel(invoiceId, getAccessToken());
              await fetchInvoices(true);
              Alert.alert('Đã hủy hóa đơn', 'Hóa đơn đã được hủy và slot được giải phóng.');
            } catch (error) {
              Alert.alert('Không thể hủy', error instanceof Error ? error.message : 'Đã xảy ra lỗi khi hủy hóa đơn.');
            } finally {
              setProcessingInvoiceId(null);
            }
          },
        },
      ]
    );
  };

  const handlePayNow = async (invoiceId: number) => {
    setProcessingInvoiceId(invoiceId);
    try {
      const response = await fetch(`${API_URL}/api/payment/create-url/${invoiceId}`, {
        method: 'POST',
        headers: { Authorization: `Bearer ${getAccessToken()}` },
      });

      if (!response.ok) {
        const responseText = await response.text();
        let message = responseText || 'Không thể tạo link thanh toán.';
        try {
          const errorData = JSON.parse(responseText);
          message = errorData.message || errorData.title || message;
        } catch {
          // Keep the server response text for non-JSON errors.
        }
        throw new Error(message);
      }

      const data = await response.json();
      const paymentUrl = data.paymentUrl;
      if (!paymentUrl) throw new Error('Không nhận được đường dẫn từ VNPay.');

      let isLinkHandled = false;
      const subscription = Linking.addEventListener('url', (event) => {
        if (!isLinkHandled && event.url.includes('payment-result')) {
          isLinkHandled = true;
          const responseCode = new URL(event.url).searchParams.get('code');
          WebBrowser.dismissBrowser();
          router.push(`/payment-result?code=${responseCode}`);
        }
      });

      try {
        const result = await WebBrowser.openBrowserAsync(paymentUrl);
        if (result.type === 'cancel' || result.type === 'dismiss') {
          await fetchInvoices(true);
        }
      } finally {
        subscription.remove();
      }
    } catch (error) {
      Alert.alert('Lỗi thanh toán', error instanceof Error ? error.message : 'Lỗi hệ thống.');
    } finally {
      setProcessingInvoiceId(null);
    }
  };

  const openCourse = (courseLevelId: number) => {
    if (!Number.isInteger(courseLevelId) || courseLevelId <= 0) {
      Alert.alert('Không thể mở khóa học', 'Hóa đơn không có thông tin khóa học hợp lệ.');
      return;
    }
    router.push(`/courses/${courseLevelId}`);
  };

  if (!isHydrated || isLoading) {
    return (
      <SafeAreaView className="flex-1 items-center justify-center bg-slate-50" edges={['top']}>
        <ActivityIndicator size="large" color="#4f46e5" />
        <Text className="mt-3 text-slate-500">Đang tải lịch sử hóa đơn...</Text>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView className="flex-1 bg-slate-50" edges={['top']}>
      <View className="flex-row items-center border-b border-slate-200 bg-white px-5 py-4">
        <TouchableOpacity
          onPress={() => router.back()}
          className="-ml-2 h-10 w-10 items-center justify-center rounded-full bg-slate-50"
          accessibilityRole="button"
          accessibilityLabel="Quay lại"
        >
          <Ionicons name="arrow-back" size={24} color="#334155" />
        </TouchableOpacity>
        <View className="ml-2 flex-1">
          <Text className="text-lg font-black text-slate-800">Hóa đơn</Text>
          <Text className="text-[11px] text-slate-500">Lịch sử thanh toán và đăng ký khóa học</Text>
        </View>
        <TouchableOpacity
          onPress={() => void fetchInvoices(true)}
          disabled={isRefreshing}
          className="h-10 w-10 items-center justify-center rounded-full bg-indigo-50"
          accessibilityRole="button"
          accessibilityLabel="Tải lại hóa đơn"
        >
          <Feather name="refresh-cw" size={18} color="#4f46e5" />
        </TouchableOpacity>
      </View>

      {errorMessage ? (
        <View className="mx-4 mt-4 rounded-2xl border border-rose-200 bg-rose-50 p-4">
          <Text className="mb-3 text-[13px] font-medium leading-5 text-rose-800">{errorMessage}</Text>
          <TouchableOpacity
            onPress={() => void fetchInvoices()}
            className="self-start rounded-xl bg-rose-600 px-4 py-2"
          >
            <Text className="text-[12px] font-bold text-white">Thử lại</Text>
          </TouchableOpacity>
        </View>
      ) : (
        <>
          <View className="mx-4 mt-4 flex-row gap-2">
            <View className="flex-1 rounded-2xl border border-slate-200 bg-white p-3">
              <Text className="text-[11px] font-medium text-slate-500">Tổng hóa đơn</Text>
              <Text className="mt-1 text-xl font-black text-slate-800">{counts.all}</Text>
            </View>
            <View className="flex-1 rounded-2xl border border-amber-200 bg-amber-50 p-3">
              <Text className="text-[11px] font-medium text-amber-800">Chờ thanh toán</Text>
              <Text className="mt-1 text-xl font-black text-amber-800">{counts.pending}</Text>
            </View>
            <View className="flex-1 rounded-2xl border border-emerald-200 bg-emerald-50 p-3">
              <Text className="text-[11px] font-medium text-emerald-800">Đã thanh toán</Text>
              <Text className="mt-1 text-xl font-black text-emerald-800">{counts.paid}</Text>
            </View>
          </View>

          <ScrollView
            className="flex-1 px-4"
            contentContainerStyle={{ paddingTop: 16, paddingBottom: 32 }}
            showsVerticalScrollIndicator={false}
            refreshControl={
              <RefreshControl
                refreshing={isRefreshing}
                onRefresh={() => void fetchInvoices(true)}
                tintColor="#4f46e5"
              />
            }
          >
            <ScrollView horizontal showsHorizontalScrollIndicator={false} className="mb-4">
              <View className="flex-row gap-2">
                {filters.map(item => {
                  const isSelected = filter === item.value;
                  return (
                    <TouchableOpacity
                      key={item.value}
                      onPress={() => setFilter(item.value)}
                      className={`rounded-full border px-4 py-2 ${isSelected ? 'border-indigo-600 bg-indigo-600' : 'border-slate-200 bg-white'}`}
                    >
                      <Text className={`text-[12px] font-bold ${isSelected ? 'text-white' : 'text-slate-600'}`}>
                        {item.label}
                      </Text>
                    </TouchableOpacity>
                  );
                })}
              </View>
            </ScrollView>

            {filteredInvoices.length === 0 ? (
              <View className="items-center rounded-2xl border border-slate-200 bg-white px-6 py-10">
                <View className="mb-4 h-20 w-20 items-center justify-center rounded-full bg-slate-100">
                  <Feather name="file-text" size={36} color="#94a3b8" />
                </View>
                <Text className="mb-2 text-base font-bold text-slate-700">
                  {invoices.length === 0 ? 'Chưa có hóa đơn nào' : 'Không có hóa đơn phù hợp'}
                </Text>
                <Text className="mb-5 text-center text-[13px] leading-5 text-slate-500">
                  {invoices.length === 0
                    ? 'Hóa đơn đăng ký và thanh toán khóa học của bé sẽ được lưu tại đây.'
                    : 'Thử chọn bộ lọc khác để xem lịch sử hóa đơn.'}
                </Text>
                {invoices.length === 0 && (
                  <TouchableOpacity
                    onPress={() => router.push('/courses')}
                    className="flex-row items-center gap-2 rounded-xl bg-indigo-600 px-5 py-3"
                  >
                    <Text className="text-[13px] font-bold text-white">Khám phá khóa học</Text>
                    <Feather name="arrow-right" size={15} color="white" />
                  </TouchableOpacity>
                )}
              </View>
            ) : (
              filteredInvoices.map(invoice => (
                <InvoiceCard
                  key={invoice.invoiceId}
                  invoice={invoice}
                  isProcessing={processingInvoiceId === invoice.invoiceId}
                  onCancel={handleCancelInvoice}
                  onPay={handlePayNow}
                  onOpenCourse={openCourse}
                />
              ))
            )}
          </ScrollView>
        </>
      )}

      {processingInvoiceId !== null && (
        <View className="absolute inset-0 z-50 items-center justify-center bg-slate-900/40">
          <View className="items-center rounded-2xl bg-white p-6 shadow-lg">
            <ActivityIndicator size="large" color="#4f46e5" />
            <Text className="mt-4 font-bold text-slate-800">Đang xử lý...</Text>
          </View>
        </View>
      )}
    </SafeAreaView>
  );
}
