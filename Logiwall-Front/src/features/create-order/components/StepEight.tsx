import { Box, Divider, Stack, ToggleButton, ToggleButtonGroup, Typography } from '@mui/material';
import { FC } from 'react';
import { useWindowSize } from 'usehooks-ts';
import * as React from 'react';
import 'swiper/css';
import 'swiper/css/pagination';
import 'swiper/css/effect-coverflow';
import { Controller, Pagination, EffectCoverflow, Navigation } from 'swiper/modules';
import { Swiper, SwiperSlide } from 'swiper/react';
import LgAnimateLoading from 'components/ui/loading/animate-loading';
import StepsButtons from 'components/ui/steps-buttons';
import LgIcon from 'components/ui/icon';
import useSelectOptions from 'features/create-order/api/select-option';
import { IgetOptions } from 'types/create-order';
import NoDataFound from 'components/ui/no-data';

// --------------------------------------------------------------------------------------------
interface IglassBox {
  icon: string;
  title: string;
  iconWidth?: number;
  largeFontSize: number;
  tinyFontSize: number;
}
const GlassBox: FC<IglassBox> = ({ icon, title, iconWidth, largeFontSize, tinyFontSize }) => {
  const { width } = useWindowSize();
  return (
    <div className="glassmorphism p-4 text-center rounded-3xl">
      <Typography fontSize={width > 800 ? largeFontSize : tinyFontSize}>
        <LgIcon width={iconWidth ?? 40} icon={icon} className="text-white inline-block mx-0.5" />
        {title}
      </Typography>
    </div>
  );
};
// -------------------------------------------------------------------------------------------

const ChooseService: FC = () => {
  const { width } = useWindowSize();

  const {
    optionsFetching,
    optionsList,
    pickupTime,
    HandleSelectOption,
    setActiveSlide,
    defaultValueOfToggleButton,
    filterProviderId,
    setPickupTime,
    SelectOptionPending,
  } = useSelectOptions();
  console.log(optionsList);
  return (
    <>
      {optionsFetching || SelectOptionPending ? (
        <LgAnimateLoading />
      ) : !optionsList ? (
        <NoDataFound />
      ) : (
        <Box pb={5}>
          <Box mt={5} mb={4}>
            <Typography fontSize={13}>
              لطفا ارائه دهنده سرویس و سرویس مورد نظر خود را انتخاب نمایید
            </Typography>
          </Box>
          <Box className={`overflow-hidden`}>
            <Swiper
              modules={[Controller, Pagination, Navigation, EffectCoverflow]}
              coverflowEffect
              effect="coverflow"
              pagination={{
                clickable: true,
                renderBullet: (index: any, className: any) => {
                  return `<span class="${className}"></span>`;
                },
              }}
              navigation
              spaceBetween={50}
              slidesPerView={1}
              loop
              onSlideChange={(e: any) => {
                const slideKeys = optionsList?.map((option: IgetOptions) => option.providerId);
                setActiveSlide(slideKeys[e.activeIndex]);
              }}
            >
              {optionsList?.map((company: any) => (
                <SwiperSlide key={company.providerId}>
                  <div
                    className="p-10 rounded-2xl bg-customColors-primaryOne shadow-md"
                    style={{ maxWidth: '800px', height: '100%', margin: 'auto' }}
                  >
                    <Stack spacing={3}>
                      <Typography color="white" variant="h2" fontSize={width > 800 ? 27 : 22}>
                        {company.providerName}
                      </Typography>
                      <Stack>
                        {company?.orderOptions?.map((option: any) => (
                          <Box className="grid grid-cols-1 gap-2" key={company?.providerId}>
                            <GlassBox
                              largeFontSize={12}
                              iconWidth={20}
                              tinyFontSize={10}
                              title={`مدت ارسال :  ${option.delivereDate}`}
                              icon="hugeicons:delivery-truck-01"
                            />
                            <GlassBox
                              largeFontSize={12}
                              iconWidth={20}
                              tinyFontSize={10}
                              title={`هزینه پایه :  ${option?.baseAmount?.toLocaleString()} ریال `}
                              icon="map:courthouse"
                            />
                            <GlassBox
                              largeFontSize={12}
                              iconWidth={15}
                              tinyFontSize={10}
                              title={`هزینه مالیات :  ${option?.taxAmount?.toLocaleString()} ریال `}
                              icon="emojione-monotone:classical-building"
                            />
                            <GlassBox
                              largeFontSize={12}
                              iconWidth={20}
                              tinyFontSize={10}
                              title={`هزینه جمع آوری :  ${option?.pickupAmount?.toLocaleString()} ریال `}
                              icon="mdi:truck"
                            />
                            <GlassBox
                              largeFontSize={22}
                              tinyFontSize={13}
                              title={`هزینه کل :  ${option?.totalAmount?.toLocaleString()} ریال `}
                              icon="lets-icons:money-duotone"
                            />
                          </Box>
                        ))}
                      </Stack>
                    </Stack>
                  </div>
                </SwiperSlide>
              ))}
            </Swiper>
          </Box>

          <Box className="mb-10 text-center">
            <Divider sx={{ my: 2 }}>
              <Box className="text-customColors-primaryOne bg-white p-4 rounded-3xl">
                <Typography>
                  <LgIcon icon="duo-icons:clock" className="inline-block mx-0.5" /> زمان جمع اوری
                </Typography>
              </Box>
            </Divider>
            <ToggleButtonGroup
              exclusive
              aria-label="text alignment"
              value={pickupTime ?? defaultValueOfToggleButton}
              onChange={(e, v) => setPickupTime(v)}
            >
              <Stack direction="row" gap={2}>
                {filterProviderId?.pickupDates?.flatMap((dates: any) => (
                  <Stack className="grid grid-cols-2" spacing={3} key={dates?.id}>
                    {dates?.times?.map((time: any) => {
                      return (
                        <ToggleButton key={time?.id} value={time?.id}>
                          <Box>
                            <Typography>{dates?.description}</Typography>
                            <Typography mt={2}>{time?.description}</Typography>
                          </Box>
                        </ToggleButton>
                      );
                    })}
                  </Stack>
                ))}
              </Stack>
            </ToggleButtonGroup>
          </Box>
          <StepsButtons onClick={HandleSelectOption} />
        </Box>
      )}
    </>
  );
};
export default ChooseService;
