import { useState } from 'react';
import { Box } from '@mui/material';
import { useForm } from 'react-hook-form';
import FormProvider from '../../../components/ui/form';
import LgMap from '../../../components/ui/map';
import StepsButtons from '../../../components/ui/steps-buttons';
import { useAppDispatch, useAppSelector } from '../../../store/store';
import { setActiveStep, setStepSixData } from '../../../store/slices/create-order-slice';
import useQueryGeoAddressing from '../../../hooks/react-query/use-query-geoaddressing';
import { toast } from 'sonner';

//-----------------------------------------------------------------------------------

function StepSix() {
  const methods = useForm({});
  const selector = useAppSelector((state) => state.CreateOrderSlice);
  const dispatch = useAppDispatch();
  const { handleSubmit } = methods;
  const { mutateAsync } = useQueryGeoAddressing();
  const [LatLong, setLatLong] = useState<[number, number]>([35.70223, 51.324606]);

  const HandleSubmit = handleSubmit(async () => {
    try {
      const response: any = await mutateAsync(LatLong);
      if (response?.data?.city !== (selector.cities.destination || 'تفت')) {
        toast.error('شهر مقصد با مختصات انتخابی مطابقت ندارد');
      } else {
        dispatch(
          setStepSixData({
            destinationLat: String(LatLong[0]),
            destinationLng: String(LatLong[1]),
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
      <Box className="mb-0.25 w-full px-0">
        <FormProvider methods={methods} onSubmit={HandleSubmit}>
          <LgMap title="مقصد خود را انتخاب کنید" LatLong={LatLong} setLatLong={setLatLong} />
          <StepsButtons />
        </FormProvider>
      </Box>
    </>
  );
}

export default StepSix;
