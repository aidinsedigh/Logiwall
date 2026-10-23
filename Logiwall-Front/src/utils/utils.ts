export const convertSecondsToMinutes = (seconds: number) => {
  const minutes = Math.floor(seconds / 60);
  const remainingSeconds = seconds % 60;
  return `${minutes}:${remainingSeconds < 10 ? '0' : ''}${remainingSeconds}`;
};

export const convertPersianNumbers = (value: string) => {
  const persianDigits = '۰۱۲۳۴۵۶۷۸۹';
  const arabicDigits = '٠١٢٣٤٥٦٧٨٩';

  return value
    .replace(/[۰-۹]/g, (w) => persianDigits.indexOf(w).toString())
    .replace(/[٠-٩]/g, (w) => arabicDigits.indexOf(w).toString());
};
