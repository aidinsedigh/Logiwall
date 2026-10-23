import { Box, Toolbar, Typography, useTheme, AppBar } from '@mui/material';
import { useNavigate } from 'react-router';
import LgIcon from '../icon';

//---------------------------------------------------------------------------------

const NavigationItems = [
  { icon: 'solar:home-smile-bold-duotone', label: 'خانه', key: '/app/home', disabled: false },
  { icon: 'solar:wallet-bold-duotone', label: 'کیف پول', key: '', disabled: true },
  { icon: 'solar:add-circle-bold-duotone', label: '', key: '/app/create-order', disabled: false },
  { icon: 'fa-solid:newspaper', label: 'سفارشات', key: '/app/orders', disabled: false },
  { icon: 'solar:user-bold-duotone', label: 'پروفایل', key: '/app/profile', disabled: false },
];

//---------------------------------------------------------------------------------

const LgAppBar = () => {
  const theme = useTheme();
  const navigate = useNavigate();

  return (
    <>
      <AppBar position="fixed" color="primary" sx={{ top: 'auto', bottom: 1 }}>
        <Toolbar
          className={`${theme.palette.mode === 'dark' ? 'bg-gray-800' : 'bg-white'} flex justify-between`}
        >
          {NavigationItems.map((item) => (
            <Box
              key={item.key}
              className={`text-center pb-3 ${item.key === '/app/create-order' || item.key === '/app/home' ? 'text-customColors-primaryOne' : `${item.disabled ? (theme.palette.mode === 'dark' ? 'opacity-20' : 'opacity-45') : 'opacity-80'} text-gray-600`}`}
              onClick={() => {
                if (item.key !== '/app/wallet') {
                  navigate(item.key);
                }
              }}
            >
              <LgIcon
                width={item.key === '/app/create-order' ? 50 : 25}
                className="inline-block"
                icon={item.icon}
              />
              <Typography
                className={`${item.key === '/app/home' ? 'text-customColors-primaryOne' : 'text-gray-400'}`}
                mt={0.5}
              >
                {item.label}
              </Typography>
            </Box>
          ))}
        </Toolbar>
      </AppBar>
    </>
  );
};
export default LgAppBar;
