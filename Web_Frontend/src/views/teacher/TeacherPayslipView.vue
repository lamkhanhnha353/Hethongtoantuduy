<script setup>
import { computed, ref, watch } from "vue";
import { ElMessage } from "element-plus";
import createApiClient from "@/services/api.service";
import authService from "@/services/auth.service";

const api = createApiClient("/api/payrolls");
const teacherId = authService.getProfileId();
const period = ref(new Date(new Date().getFullYear(), new Date().getMonth(), 1));
const detail = ref(null);
const loading = ref(false);

const payslip = computed(() => detail.value?.payslip ?? null);
const sessions = computed(() => detail.value?.sessions ?? []);
const adjustments = computed(() => detail.value?.adjustments ?? []);
const bonusTotal = computed(() => adjustments.value
  .filter((item) => item.type === "Bonus")
  .reduce((total, item) => total + item.amount, 0));
const penaltyTotal = computed(() => adjustments.value
  .filter((item) => item.type === "Penalty")
  .reduce((total, item) => total + item.amount, 0));

const periodQuery = computed(() => ({
  month: period.value.getMonth() + 1,
  year: period.value.getFullYear(),
}));

const formatCurrency = (value) =>
  new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(value ?? 0);

const formatDate = (value) => {
  if (!value) return "—";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "—" : new Intl.DateTimeFormat("vi-VN").format(date);
};

function getErrorMessage(error, fallback) {
  return error?.response?.data?.detail
    ?? error?.response?.data?.message
    ?? error?.message
    ?? fallback;
}

async function loadPayslip() {
  if (!teacherId) {
    detail.value = null;
    ElMessage.error("Không xác định được hồ sơ giáo viên của tài khoản hiện tại. Vui lòng đăng nhập lại.");
    return;
  }

  loading.value = true;
  try {
    const response = await api.get("/detail", {
      params: {
        teacherId,
        ...periodQuery.value,
      },
    });
    detail.value = response.data;
  } catch (error) {
    detail.value = null;
    ElMessage.error(getErrorMessage(error, "Không thể tải phiếu lương."));
  } finally {
    loading.value = false;
  }
}

watch(period, loadPayslip, { immediate: true });
</script>

<template>
  <main v-loading="loading" class="payslip-page">
    <header class="page-header">
      <div>
        <p class="eyebrow">Thu nhập cá nhân</p>
        <h1>Phiếu lương giáo viên</h1>
        <p class="page-description">Theo dõi lương theo số buổi dạy hợp lệ và các khoản điều chỉnh minh bạch.</p>
      </div>
      <el-date-picker
        v-model="period"
        type="month"
        format="MM/YYYY"
        placeholder="Chọn tháng"
        :clearable="false"
        aria-label="Chọn tháng tra cứu lương"
      />
    </header>

    <template v-if="payslip">
      <section v-loading="loading" class="summary-grid">
        <el-card class="summary-card net-card" shadow="hover">
          <div class="stat-top"><span class="stat-icon">₫</span><span class="stat-label">Tổng thực nhận</span></div>
          <strong class="stat-value">{{ formatCurrency(payslip.finalSalary) }}</strong>
          <span class="stat-note">Kỳ lương {{ String(payslip.month).padStart(2, "0") }}/{{ payslip.year }}</span>
        </el-card>
        <el-card class="summary-card" shadow="hover">
          <div class="stat-top"><span class="stat-icon session-icon">✓</span><span class="stat-label">Buổi dạy hợp lệ</span></div>
          <strong class="stat-value">{{ payslip.totalValidSessions }} <small>buổi</small></strong>
          <span class="stat-note">Đã hoàn thành và có điểm danh</span>
        </el-card>
        <el-card class="summary-card" shadow="hover">
          <div class="stat-top"><span class="stat-icon adjustment-icon">±</span><span class="stat-label">Thưởng / Phạt</span></div>
          <div class="adjustment-summary">
            <span class="bonus">+{{ formatCurrency(payslip.totalBonus) }}</span>
            <span class="penalty">−{{ formatCurrency(payslip.totalPenalty) }}</span>
          </div>
          <span class="stat-note">Tổng thưởng và tổng khấu trừ trong kỳ</span>
        </el-card>
      </section>

      <section class="content-card">
        <div class="section-heading">
          <div>
            <h2>Chi tiết buổi dạy</h2>
            <p>Mỗi buổi hoàn thành có điểm danh được tính một đơn vị lương.</p>
          </div>
          <el-tag type="success" effect="light">{{ sessions.filter((item) => item.isEligible).length }} buổi tính lương</el-tag>
        </div>
        <el-table v-loading="loading" :data="sessions" stripe>
          <el-table-column label="Ngày dạy" min-width="140">
            <template #default="{ row }">{{ formatDate(row.lessonDate) }}</template>
          </el-table-column>
          <el-table-column label="Bài học / Chủ đề" min-width="220">
            <template #default="{ row }">{{ row.title || "Chưa đặt tên" }}</template>
          </el-table-column>
          <el-table-column label="Trạng thái" width="145">
            <template #default="{ row }">
              <el-tag :type="row.status === 'COMPLETED' ? 'success' : 'info'" effect="light">{{ row.status }}</el-tag>
            </template>
          </el-table-column>
          <el-table-column label="Điểm danh" width="150">
            <template #default="{ row }">
              <span :class="row.hasAttendance ? 'attendance-done' : 'attendance-missing'">
                {{ row.hasAttendance ? "Đã điểm danh" : "Chưa điểm danh" }}
              </span>
            </template>
          </el-table-column>
          <el-table-column label="Tính lương" width="140" align="center">
            <template #default="{ row }">
              <el-tag :type="row.isEligible ? 'success' : 'info'" effect="light">
                {{ row.isEligible ? "+1 Đơn vị" : "0" }}
              </el-tag>
            </template>
          </el-table-column>
          <template #empty><el-empty description="Chưa có buổi học trong tháng này." /></template>
        </el-table>
      </section>

      <section class="content-card">
        <div class="section-heading">
          <div>
            <h2>Khoản thưởng / phạt</h2>
            <p>Danh sách điều chỉnh lương cùng lý do được ghi nhận bởi Admin.</p>
          </div>
        </div>
        <el-table :data="adjustments" stripe>
          <el-table-column label="Ngày áp dụng" width="150">
            <template #default="{ row }">{{ formatDate(row.appliedDate) }}</template>
          </el-table-column>
          <el-table-column label="Loại" width="125">
            <template #default="{ row }">
              <el-tag :type="row.type === 'Bonus' ? 'success' : 'danger'" effect="light">
                {{ row.type === "Bonus" ? "Thưởng" : "Phạt" }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="Số tiền" width="180" align="right">
            <template #default="{ row }">
              <strong :class="row.type === 'Bonus' ? 'bonus' : 'penalty'">
                {{ row.type === "Bonus" ? "+" : "−" }}{{ formatCurrency(row.amount) }}
              </strong>
            </template>
          </el-table-column>
          <el-table-column prop="reason" label="Lý do" min-width="280" />
          <template #empty><el-empty description="Không có khoản thưởng hoặc phạt trong kỳ." /></template>
        </el-table>
      </section>
    </template>

    <el-card v-else-if="!loading" class="empty-card" shadow="never">
      <el-empty description="Không có dữ liệu phiếu lương cho tháng đã chọn." />
    </el-card>
  </main>
</template>

<style scoped>
.payslip-page { display: grid; gap: 22px; max-width: 1280px; margin: 0 auto; padding-bottom: 36px; color: #1f2937; }
.page-header { display: flex; align-items: flex-end; justify-content: space-between; gap: 18px; flex-wrap: wrap; }
.eyebrow { margin: 0 0 5px; color: #ea580c; font-size: 11px; font-weight: 800; letter-spacing: .14em; text-transform: uppercase; }
h1 { margin: 0; color: #431407; font-size: clamp(24px, 3vw, 32px); font-weight: 800; letter-spacing: -.03em; }
.page-description { margin: 7px 0 0; color: #6b7280; font-size: 14px; }
.summary-grid { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 14px; }
.summary-card { min-height: 150px; border: 1px solid #ffedd5; border-radius: 17px; }
.summary-card :deep(.el-card__body) { display: flex; height: 100%; flex-direction: column; padding: 18px; }
.net-card { background: linear-gradient(135deg, #fff7ed, #fff); }
.stat-top { display: flex; align-items: center; gap: 9px; }
.stat-icon { display: grid; width: 32px; height: 32px; place-items: center; border-radius: 10px; background: #ffedd5; color: #c2410c; font-size: 18px; font-weight: 800; }
.session-icon { background: #dcfce7; color: #15803d; }
.adjustment-icon { background: #f3e8ff; color: #7e22ce; }
.stat-label { color: #6b7280; font-size: 13px; font-weight: 600; }
.stat-value { margin-top: 13px; color: #292524; font-size: clamp(20px, 3vw, 27px); font-weight: 800; }
.net-card .stat-value { color: #9a3412; }
.stat-value small { color: #6b7280; font-size: 14px; font-weight: 600; }
.stat-note { margin-top: auto; padding-top: 8px; color: #9ca3af; font-size: 11px; }
.adjustment-summary { display: flex; flex-wrap: wrap; gap: 6px 16px; margin-top: 13px; font-size: 14px; font-weight: 750; }
.bonus { color: #15803d; }
.penalty { color: #dc2626; }
.content-card { overflow: hidden; border: 1px solid #ffedd5; border-radius: 18px; background: white; box-shadow: 0 8px 24px rgb(124 45 18 / 5%); }
.section-heading { display: flex; align-items: center; justify-content: space-between; gap: 12px; padding: 18px 20px; }
.section-heading h2 { margin: 0; color: #431407; font-size: 17px; font-weight: 800; }
.section-heading p { margin: 5px 0 0; color: #9ca3af; font-size: 12px; }
.attendance-done { color: #15803d; font-size: 12px; font-weight: 650; }
.attendance-missing { color: #9ca3af; font-size: 12px; }
.empty-card { border: 1px solid #ffedd5; border-radius: 18px; }
.empty-card :deep(.el-card__body) { padding: 12px; }
:deep(.el-table th.el-table__cell) { background: #fff7ed; color: #7c2d12; font-size: 12px; }
@media (max-width: 760px) {
  .summary-grid { grid-template-columns: 1fr; }
  .summary-card { min-height: 130px; }
}
</style>
