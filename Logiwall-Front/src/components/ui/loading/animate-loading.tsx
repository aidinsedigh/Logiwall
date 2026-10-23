import { Box, useTheme } from '@mui/material';
import animate from './animate.json';
import Lottie from 'lottie-react';

// -------------------------------------------------------------

const LgAnimateLoading = () => {
  const theme = useTheme();
  return (
    <>
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'center',
          height: '100vh', // Full viewport height
          width: '100vw', // Full viewport width
          position: 'fixed', // Fixed positioning
          top: 0,
          left: 0,
          backgroundColor: theme.palette.mode === 'dark' ? '#1f2937' : '#ffff', // Optional background color
          zIndex: 9999, // Ensure it appears above other content
        }}
      >
        <Lottie animationData={animate} loop={true} />
      </Box>
    </>
  );
};
export default LgAnimateLoading;
