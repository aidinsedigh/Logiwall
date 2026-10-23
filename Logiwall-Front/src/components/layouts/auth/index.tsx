import * as React from 'react';
import Box from '@mui/material/Box';
import logo from '../../../assets/images/Screenshot 2024-09-27 195349.png';
import banner from '../../../assets/images/Screenshot 2024-08-20 041937.png';
import tipax from '../../../assets/images/tipax.jpg';
import tinex from '../../../assets/images/tnext.jpg';
import tiexpress from '../../../assets/images/tiexpress.png';
import { Swiper, SwiperSlide } from 'swiper/react';
import { Typography } from '@mui/material';
import 'swiper/css'; // Swiper styles
import 'swiper/css/pagination'; // Pagination styles

// Import Swiper styles
import 'swiper/css';
import 'swiper/css/navigation';
import 'swiper/css/pagination';
import { Autoplay } from 'swiper/modules';
import { useWindowSize } from 'usehooks-ts';
import { FC } from 'react';
import { Ichildren } from '../../../types/global';

//--------------------------------------------------------------------------------------------

const AuthLayout: FC<Ichildren> = ({ children }) => {
  const { width } = useWindowSize();

  return (
    <Box className="flex justify-center items-center h-screen">
      <Box className="container mx-auto">
        <Box className="text-center border-none md:border shadow-none md:shadow-md rounded-xl p-0">
          <Box className="flex justify-center items-center">
            {width > 900 && (
              <Box className="flex flex-col items-center border-l-2 w-full md:w-1/2">
                <img src={banner} className="w-40 md:w-full" alt="Logiwall" />
                <Box className="w-full my-10">
                  <Typography
                    className="text-black text-right"
                    fontSize={18}
                    variant="body1"
                    mx={2}
                  >
                    سرویس دهندگان :{' '}
                  </Typography>
                  <Swiper
                    modules={[Autoplay]}
                    spaceBetween={50}
                    slidesPerView={1}
                    autoplay={{ delay: 3000 }}
                    className="w-full"
                    loop
                  >
                    <SwiperSlide>
                      <img src={tipax} className="w-24 inline-block" alt="tipax" />
                    </SwiperSlide>
                    <SwiperSlide>
                      <img src={tiexpress} className="w-24 inline-block" alt="tiexpress" />
                    </SwiperSlide>
                    <SwiperSlide>
                      <img src={tinex} className="w-24 inline-block" alt="tinex" />
                    </SwiperSlide>
                  </Swiper>
                </Box>
                <Typography variant="caption" mb={2} fontSize={11}>
                  تمامی حقوق برای شرکت فناوران پیشرو ثمین محفوظ میباشد.
                </Typography>
              </Box>
            )}
            <Box className="w-full text-center mb-28 px-3">
              <img src={logo} className="w-28 mb-10 mt-10 inline-block" alt="Logiwall" />
              {children}
            </Box>
          </Box>
        </Box>
      </Box>
    </Box>
  );
};
export default AuthLayout;
