import * as Linking from 'expo-linking';
import * as WebBrowser from 'expo-web-browser';

function extractResponseCode(url: string): string | null {
  try {
    const parsed = Linking.parse(url);
    const rawCode = parsed.queryParams?.code;
    if (Array.isArray(rawCode)) {
      return rawCode[0] ?? null;
    }
    if (typeof rawCode === 'string') {
      return rawCode;
    }
  } catch {
    // Fallback sang regex nếu URL có định dạng đặc biệt
  }

  const match = /[?&]code=([^&#]+)/.exec(url);
  return match ? decodeURIComponent(match[1]) : null;
}

export async function openPaymentUrl(
  paymentUrl: string,
  onReturn: (responseCode: string | null) => void,
) {
  const returnUrl = Linking.createURL('payment-result');
  const result = await WebBrowser.openAuthSessionAsync(paymentUrl, returnUrl);

  if (result.type === 'success') {
    const responseCode = extractResponseCode(result.url);
    onReturn(responseCode);
  }

  return result;
}
