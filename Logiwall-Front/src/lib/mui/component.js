import { grey, primary } from './palette';

export const components = {
  MuiToggleButton: {
    styleOverrides: {
      root: {
        '&.Mui-selected': {
          backgroundColor: '#4bceaf', // Change this to the desired active background color
          color: '#ffffff', // Change the text color when active
          '&:hover': {
            backgroundColor: '#4bceaf', // Change the hover color when the button is active
          },
        },
      },
    },
  },
};
