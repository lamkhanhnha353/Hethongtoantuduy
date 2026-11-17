export type InvoiceStatus = 'PENDING' | 'PAID' | 'FAILED' | 'CANCELLED' | 'REFUNDED';

export interface Invoice {
  invoiceId: number;
  amount: number;
  status: InvoiceStatus;
  createdAt: string;
  paidAt: string | null;
  txnRef: string;
  paymentMethod: string | null;
  classId: number;
  courseLevelId: number;
  courseLevelName: string;
  className: string;
  studentName: string;
}

export type InvoiceFilter = 'ALL' | 'PENDING' | 'PAID' | 'OTHER';
