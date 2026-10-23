import { Box, Stack, Typography } from '@mui/material';
import notFoundImg from 'assets/images/freepik_br_9c2b6e3e-2917-4f0f-a1ec-3dfacea2c72b.png';
import LgButton from 'components/ui/button';
import { useNavigate } from 'react-router';

// ----------------------------------------------------------------------------------------------

export const Error404Page = () => {
  const navigate = useNavigate();

  return (
    <Stack justifyContent="center" sx={{ height: '100vh', width: '100%' }}>
      <img src={notFoundImg} alt="404 image" />
      <Typography textAlign="center" fontSize={15}>
        صفحه مورد نظر یافت نشد
      </Typography>
      <Box textAlign="center" mt={5}>
        <LgButton fullWidth={false} onClick={() => navigate('/app/home', { replace: true })}>
          بازگشت به صفحه اصلی
        </LgButton>
      </Box>
    </Stack>
  );
};
