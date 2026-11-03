import * as Linking from 'expo-linking';
import * as WebBrowser from 'expo-web-browser';

export async function openPaymentUrl(
  paymentUrl: string,
  onReturn: (responseCode: string | null) => void,
) {
  let isLinkHandled = false;
  const subscription = Linking.addEventListener('url', ({ url }) => {
    if (isLinkHandled || !url.includes('payment-result')) return;

    isLinkHandled = true;
    const responseCode = new URL(url).searchParams.get('code');
    WebBrowser.dismissBrowser();
    onReturn(responseCode);
  });

  try {
    return await WebBrowser.openBrowserAsync(paymentUrl);
  } finally {
    subscription.remove();
  }
}
