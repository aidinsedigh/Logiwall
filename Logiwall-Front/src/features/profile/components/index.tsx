import { Box, Stack, Typography } from '@mui/material';
import { useState } from 'react';

import ChangeUserInfo from './change-user-info';
import useLogout from '../../../hooks/use-auth-logout';
import LgIcon from '../../../components/ui/icon';
import { useNavigate } from 'react-router';

//----------------------------------------------------------------------------------------------

const Profile = () => {
  const [modeOfProfile, setModeOfProfile] = useState('menu');

  const { Logout } = useLogout();
  const navigate = useNavigate();

  return (
    <>
      {modeOfProfile === 'menu' && (
        <Stack spacing={3}>
          <Box
            onClick={() => setModeOfProfile('changeInfo')}
            className="bg-customColors-primaryTwo flex justify-between p-4 rounded-2xl text-customColors-primaryOne"
          >
            <Box>
              <LgIcon className="inline-block" icon="solar:user-id-bold-duotone" />
              <Typography color="black" ml={1} className="inline-block">
                {' '}
                تغییر اطلاعات کاربری{' '}
              </Typography>
            </Box>
            <LgIcon icon="solar:alt-arrow-left-line-duotone" />
          </Box>
          <Box
            className="bg-customColors-primaryTwo flex justify-between p-4 rounded-2xl text-customColors-primaryOne"
            onClick={() => navigate('/app/change-password', { replace: true })}
          >
            <Box>
              <LgIcon className="inline-block" icon="solar:lock-password-bold-duotone" />
              <Typography color="black" ml={1} className="inline-block">
                {' '}
                تغییر رمز عبور{' '}
              </Typography>
            </Box>
            <LgIcon icon="solar:alt-arrow-left-line-duotone" />
          </Box>
          <Box
            onClick={() => Logout()}
            className="bg-customColors-primaryTwo flex justify-between p-4 rounded-2xl text-red-700"
          >
            <Box>
              <LgIcon className="inline-block" icon="solar:logout-2-bold-duotone" />
              <Typography color="black" ml={1} className="inline-block">
                {' '}
                خروج از حساب کاربری{' '}
              </Typography>
            </Box>
          </Box>
        </Stack>
      )}
      {modeOfProfile === 'changeInfo' && <ChangeUserInfo />}
    </>
  );
};
export default Profile;
