import * as React from 'react';
import Box from '@mui/material/Box';
import { Stack, Typography } from '@mui/material';
import 'swiper/css';
import 'swiper/css/pagination';
import 'swiper/css/effect-cards';
import { useWindowSize } from 'usehooks-ts';
import { FC } from 'react';
import { useNavigate } from 'react-router';
import LgAppBar from 'components/ui/app-bar';
import LgIcon from 'components/ui/icon';
import useApiEndpoints from 'hooks/use-api-endpoints';
import { axiosInternal } from 'lib/axios';
import { useQuery } from '@tanstack/react-query';
import NoDataFound from '../../../components/ui/no-data';
import Order from '../../Orders/components/Order';
//---------------------------------------------------------------------------------
const Home: FC = () => {
  const { width } = useWindowSize();
  const endpoints = useApiEndpoints();
  const { data } = useQuery({
    queryKey: ['orders-list'],
    queryFn: () =>
      axiosInternal.post(endpoints.GET_ORDERS.GET_ORDERS_LIST, {
        hasPagination: false,
        pageNumber: 0,
        pageSize: 0,
      }),
    enabled: window.location.pathname === '/app/home',
  });
  const navigate = useNavigate();
  return (
    <>
      <Stack className="container mx-auto">
        <Box>
          <Box>
            <Typography fontSize={13} fontWeight="bold">
              سرویس های ما
            </Typography>
          </Box>
          <Box className="grid grid-cols-3 gap-8 mt-8">
            <Box
              className="text-center cursor-pointer"
              onClick={() => navigate('/app/parcel-tracking')}
            >
              <Box className="bg-customColors-primaryOne inline-block p-3 rounded-lg text-white">
                <LgIcon width={30} icon="solar:qr-code-bold-duotone" />
              </Box>
              <Typography mt={1}>رهگیری مرسوله</Typography>
            </Box>
            <Box
              className="text-center cursor-pointer"
              onClick={() => navigate('/app/create-order')}
            >
              <Box className="bg-customColors-primaryOne inline-block p-3 rounded-lg text-white">
                <LgIcon width={30} icon="solar:box-bold-duotone" />
              </Box>
              <Typography mt={1}>ثبت سفارش</Typography>
            </Box>
            <Box className="text-center cursor-pointer" onClick={() => navigate('/app/orders')}>
              <Box className="bg-customColors-primaryOne inline-block p-3 rounded-lg text-white">
                <LgIcon width={30} icon="solar:documents-minimalistic-bold-duotone" />
              </Box>
              <Typography mt={1}>لیست سفارشات</Typography>
            </Box>
            <Box className="text-center">
              <Box className="bg-gray-200 inline-block p-3 rounded-lg text-gray-500">
                <LgIcon width={30} icon="solar:calculator-bold-duotone" />
              </Box>
              <Typography mt={1} className="text-gray-500">
                محاسبه قیمت
              </Typography>
            </Box>
            <Box className="text-center">
              <Box className="bg-gray-200 inline-block p-3 rounded-lg text-gray-500">
                <LgIcon width={30} icon="solar:info-circle-bold-duotone" />
              </Box>
              <Typography mt={1} className="text-gray-500">
                درباره ما
              </Typography>
            </Box>
          </Box>
        </Box>
        <Box mt={5} pb={10}>
          <Box mb={2}>
            <Typography fontWeight="bold" fontSize={13}>
              سفارشات فعال
            </Typography>
          </Box>
          {!data ? <NoDataFound /> : <Order order={data?.data?.data?.result[0]} />}
          {data?.data?.data?.result?.length > 1 && (
            <Box mt={5} onClick={() => navigate('/app/orders', { replace: true })}>
              <Stack spacing={2} textAlign="center">
                <Typography className="text-customColors-primaryOne">
                  <LgIcon
                    icon={'solar:double-alt-arrow-down-bold-duotone'}
                    className={`inline-block ml-1`}
                  />
                  مشاهده تمام سفارشات
                </Typography>
              </Stack>
            </Box>
          )}
        </Box>
      </Stack>
      {width < 800 && <LgAppBar />}
    </>
  );
};
export default Home;
