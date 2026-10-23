import { Box, Drawer, List, ListItem, Stack, Tooltip, Typography, useTheme } from '@mui/material';
import { FC, useState } from 'react';
import greenLogo from 'assets/images/Screenshot 2024-09-27 195349.png';
import { useWindowSize } from 'usehooks-ts';
import whiteLogo from 'assets/images/Screenshot 2024-09-28 033400.png';
import * as React from 'react';
import banner from 'assets/images/logisticland.png';
import 'swiper/css';
import 'swiper/css/pagination';
import 'swiper/css/effect-cards';
import { Autoplay, Controller, Pagination, EffectCards } from 'swiper/modules';
import { Swiper, SwiperSlide } from 'swiper/react';
import { useNavigate } from 'react-router';
import { useAppDispatch, useAppSelector } from 'store/store';
import useQueryProfile from 'hooks/react-query/use-query-profile';
import { setResetRegisterSlice } from 'store/slices/create-order-slice';
import LgIcon from '../../ui/icon';
import { useThemeContext } from 'lib/mui';
import tipaxLogo from 'assets/images/Screenshot 2024-09-08 010846_prev_ui.png';
//--------------------------------------------------------------------------------------------

const ListContent = [
  { label: 'خانه', icon: 'solar:home-smile-bold-duotone', path: '/app/home' },
  { label: 'پروفایل', icon: 'solar:user-bold-duotone', path: '/app/profile' },
  { label: 'کیف پول', icon: 'solar:wallet-bold-duotone', path: '' },
  { label: 'ثبت سفارش', icon: 'teenyicons:box-solid', path: '/app/create-order' },
  { label: 'لیست سفارشات', icon: 'arcticons:parcels', path: '' },
  { label: 'رهگیری مرسوله', icon: 'hugeicons:delivery-truck-02', path: '/app/parcel-tracking' },
  { label: 'محاسبه قیمت', icon: 'material-symbols:calculate', path: '' },
  { label: 'تماس با ما', icon: 'zondicons:exclamation-solid', path: '' },
];

//--------------------------------------------------------------------------------------------

const LgHeader: FC = () => {
  const { toggleTheme } = useThemeContext();
  const theme = useTheme();
  const [menuStatus, setMenuStatus] = useState(false);
  const { width } = useWindowSize();
  const currentPath: any = window.location.pathname;
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const selector: any = useAppSelector((state) => state.TrackingSlice.trackingData);

  const { data } = useQueryProfile();
  return (
    <>
      <header
        className={`px-5 py-3 ${theme.palette.mode === 'dark' ? 'bg-gray-800' : 'bg-customColors-primaryOne'}`}
      >
        <Stack direction="row" justifyContent="space-between">
          <Box>
            {width > 800 ? (
              <LgIcon
                width={36}
                icon="solar:hamburger-menu-bold-duotone"
                className="cursor-pointer text-white"
                onClick={() => setMenuStatus(true)}
              />
            ) : (
              <img src={whiteLogo} className="w-14" />
            )}
          </Box>
          <Box className="flex justify-center items-center">
            {currentPath === '/app/home' ? (
              <Tooltip title={theme.palette.mode == 'dark' ? 'روز' : 'شب'}>
                <LgIcon
                  onClick={toggleTheme}
                  width={20}
                  sx={{ color: 'gray', cursor: 'pointer' }}
                  icon={
                    theme.palette.mode == 'dark'
                      ? 'icon-park-twotone:sun-one'
                      : 'solar:cloudy-moon-bold-duotone'
                  }
                />
              </Tooltip>
            ) : (
              <LgIcon
                width={30}
                onClick={() => {
                  navigate('/app/home', { replace: true });
                  dispatch(setResetRegisterSlice());
                }}
                className="text-white cursor-pointer"
                icon="solar:alt-arrow-left-broken"
              />
            )}
          </Box>
        </Stack>
      </header>
      {currentPath !== '/app/create-order' && currentPath !== '/app/orders' && (
        <Box
          className={`${theme.palette.mode === 'dark' ? 'bg-gray-800' : 'bg-customColors-primaryOne'} ${currentPath === '/order-registration' ? 'p-0' : 'p-10'} overflow-hidden`}
        >
          {currentPath === '/app/parcel-tracking/result' && (
            <Stack className="flex justify-center items-center text-white">
              <div
                className="glassmorphism w-72 md:w-80 shadow-md px-4 py-4"
                style={{ margin: 'auto' }}
              >
                <Box className="flex justify-between">
                  <img src={tipaxLogo} className="w-14 md:w-32" />
                  <Typography color="white" fontSize={11}>
                    {selector?.ContractKindName}
                  </Typography>
                </Box>
                <Box className="flex justify-between" mt={2}>
                  <Box>
                    <Typography color="white" fontSize={10}>
                      مبدا :
                    </Typography>
                    <Typography color="white" fontSize={17}>
                      {selector?.SenderCityName}
                    </Typography>
                  </Box>
                  <Box mt={1}>
                    <Typography>
                      <LgIcon icon="svg-spinners:clock" width={16} className="inline-block mx-1" />{' '}
                      در حال ارسال{' '}
                    </Typography>
                  </Box>
                  <Box>
                    <Typography color="white" fontSize={10}>
                      مقصد :
                    </Typography>
                    <Typography color="white" fontSize={17}>
                      {selector?.ReceiverCityName}
                    </Typography>
                  </Box>
                </Box>
              </div>
            </Stack>
          )}
          {currentPath === '/app/parcel-tracking' && (
            <Stack className="flex justify-center items-center text-white">
              <Box>
                <LgIcon width={80} icon="solar:qr-code-bold-duotone" />
                <Typography fontSize={15} mt={2}>
                  رهگیری مرسوله
                </Typography>
              </Box>
            </Stack>
          )}
          {currentPath === '/app/profile' ||
            (currentPath === '/app/change-password' && (
              <Stack>
                <Box className="flex justify-center items-center text-white">
                  <LgIcon width={80} icon="solar:user-id-bold-duotone" />
                </Box>
                <Box className="flex justify-center items-center text-white">
                  <Typography fontSize={15} mt={2}>
                    {data?.data?.firstName} {data?.data?.lastName}
                  </Typography>
                </Box>
              </Stack>
            ))}
          {currentPath === '/app/price-calculation' && (
            <Stack className="flex justify-center items-center text-white">
              <Box>
                <LgIcon width={80} icon="solar:calculator-bold-duotone" />
                <Typography fontSize={15} mt={2}>
                  محاسبه قیمت
                </Typography>
              </Box>
            </Stack>
          )}
          {currentPath === '/app/home' && (
            <Swiper
              modules={[Autoplay, Controller, Pagination, EffectCards]}
              cardsEffect
              effect="cards"
              pagination={{
                clickable: true,
                renderBullet: (index: any, className: any) => {
                  return `<span class="${className}"></span>`;
                },
              }}
              spaceBetween={50}
              slidesPerView={1}
              autoplay={{ delay: 15000 }}
              loop
            >
              <SwiperSlide>
                <div
                  className="glassmorphism px-4 flex justify-between shadow-md items-center"
                  style={{ maxWidth: '600px', margin: 'auto' }}
                >
                  <Box>
                    <Typography color="white" fontSize={width > 800 ? 20 : 15}>
                      لاجی وال
                    </Typography>
                    <Typography color="white" fontSize={width > 800 ? 12 : 10}>
                      با انتخابی هوشمند ، ارسالی سریع و اسان
                    </Typography>
                  </Box>
                  <img src={banner} className="w-32 md:w-48" />
                </div>
              </SwiperSlide>
              <SwiperSlide>
                <div
                  className="glassmorphism px-4 flex justify-between shadow-md items-center"
                  style={{ maxWidth: '600px', margin: 'auto' }}
                >
                  <Box>
                    <Typography color="white" fontSize={width > 800 ? 20 : 15}>
                      لاجی وال
                    </Typography>
                    <Typography color="white" fontSize={width > 800 ? 12 : 10}>
                      با انتخابی هوشمند ، ارسالی سریع و اسان
                    </Typography>
                  </Box>
                  <img src={banner} className="w-28 md:w-48" />
                </div>
              </SwiperSlide>
              <SwiperSlide>
                <div
                  className="glassmorphism px-4 flex justify-between shadow-md items-center"
                  style={{ maxWidth: '600px', margin: 'auto' }}
                >
                  <Box>
                    <Typography color="white" fontSize={width > 800 ? 20 : 15}>
                      لاجی وال
                    </Typography>
                    <Typography color="white" fontSize={width > 800 ? 12 : 10}>
                      با انتخابی هوشمند ، ارسالی سریع و اسان
                    </Typography>
                  </Box>
                  <img src={banner} className="w-32 md:w-48" />
                </div>
              </SwiperSlide>
            </Swiper>
          )}
        </Box>
      )}
      <Drawer open={menuStatus} onClose={() => setMenuStatus(false)}>
        <Box
          className={`text-center h-screen ${theme.palette.mode === 'dark' ? 'bg-gray-800' : 'bg-white'}`}
        >
          <img src={greenLogo} className="mb-12 w-24 mt-5 inline-block" />
          <List className="space-y-7">
            {ListContent.map((content) => {
              return (
                <ListItem
                  className="cursor-pointer"
                  onClick={() => {
                    navigate(content.path);
                    setMenuStatus(false);
                  }}
                  key={content.icon}
                >
                  <LgIcon className="text-customColors-primaryOne" icon={content.icon} />
                  <Typography fontSize={14} mx={3}>
                    {content.label}
                  </Typography>
                </ListItem>
              );
            })}
          </List>
        </Box>
      </Drawer>
    </>
  );
};
export default LgHeader;
