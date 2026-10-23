/** @type {import('tailwindcss').Config} */
module.exports = {
  content: ['./src/**/*.{js,jsx,ts,tsx}'],
  theme: {
    extend: {
      colors: { customColors: { primaryOne: '#4bceaf', primaryTwo: '#E6F5F4' } },
    },
  },
  plugins: [],
};
