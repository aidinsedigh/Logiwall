import React, { useState } from 'react';
import { Box } from '@mui/material';
import { useForm } from 'react-hook-form';
import { useAppDispatch, useAppSelector } from 'store/store';
import { setActiveStep, setStepFourData } from 'store/slices/create-order-slice';
import FormProvider from 'components/ui/form';
import LgMap from 'components/ui/map';
import StepsButtons from 'components/ui/steps-buttons';
import useQueryGeoAddressing from 'hooks/react-query/use-query-geoaddressing';
import { toast } from 'sonner';

//-------------------------------------------------------------------------

function StepFour() {
  const methods = useForm({});
  const selector: any = useAppSelector((state) => state.CreateOrderSlice);
  const dispatch = useAppDispatch();
  const { mutateAsync } = useQueryGeoAddressing();

  const { handleSubmit } = methods;
  const [LatLong, setLatLong] = useState<[number, number]>([35.70223, 51.324606]);

  const HandleSubmit = handleSubmit(async () => {
    try {
      const response: any = await mutateAsync(LatLong);
      if (response?.data?.city !== (selector.cities.source || 'تهران')) {
        toast.error('شهر مبدا با مختصات انتخابی مطابقت ندارد');
      } else {
        dispatch(
          setStepFourData({
            originLat: String(LatLong[0]),
            originLng: String(LatLong[1]),
          })
        );
        dispatch(setActiveStep(selector.activeStep + 1));
      }
    } catch (e) {
      console.log(e);
    }
  });

  return (
    <>
      <Box>
        <Box className="mb-0.25 w-full px-0">
          <FormProvider methods={methods} onSubmit={HandleSubmit}>
            <LgMap title="مبدا خود را انتخاب کنید" LatLong={LatLong} setLatLong={setLatLong} />
            <StepsButtons />
          </FormProvider>
        </Box>
      </Box>
    </>
  );
}

export default StepFour;
