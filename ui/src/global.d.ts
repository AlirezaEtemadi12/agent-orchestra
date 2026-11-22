export {};

declare module 'next-intl' {
  interface AppConfig {
    Locale: 'fa' | 'en';
    Messages: typeof import('@/i18n/messages').messages.en;
  }
}
