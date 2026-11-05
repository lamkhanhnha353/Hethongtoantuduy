import * as Linking from 'expo-linking';
import * as WebBrowser from 'expo-web-browser';

export async function openPaymentUrl(
  paymentUrl: string,
  onReturn: (responseCode: string | null) => void,
) {
  const returnUrl = Linking.createURL('payment-result');
  const result = await WebBrowser.openAuthSessionAsync(paymentUrl, returnUrl);

  if (result.type === 'success') {
    const responseCode = new URL(result.url).searchParams.get('code');
    onReturn(responseCode);
  }

  return result;
}
