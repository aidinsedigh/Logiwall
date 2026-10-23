import { Stack } from '@mui/material';
import { useForm } from 'react-hook-form';
import { useAppDispatch, useAppSelector } from 'store/store';
import { setActiveStep, setStepOneData } from 'store/slices/create-order-slice';
import StepTitle from 'components/ui/title';
import FormProvider from 'components/ui/form';
import StepsButtons from 'components/ui/steps-buttons';
import SelectCity from 'components/basic-info/select-city';

//--------------------------------------------------------------------------------------------------

const StepOne = () => {
  const dispatch = useAppDispatch();
  const selector = useAppSelector((state) => state.CreateOrderSlice.activeStep);

  const methods = useForm({
    defaultValues: {
      originCity: 2,
      destinationCity: 100,
    },
  });
  const { control, handleSubmit } = methods;

  const HandleSubmit = handleSubmit((payloads) => {
    dispatch(setStepOneData(payloads));
    dispatch(setActiveStep(selector + 1));
  });

  return (
    <>
      <StepTitle
        className="text-customColors-primaryOne"
        icon={'solar:point-on-map-bold-duotone'}
        title={' لطفا مبدا و مقصد خود را انتخاب کنید'}
      />
      <FormProvider methods={methods} onSubmit={HandleSubmit}>
        <Stack spacing={4}>
          <SelectCity name={'originCity'} control={control} isOrigin />
          <SelectCity name={'destinationCity'} control={control} isOrigin={false} />
        </Stack>
        <StepsButtons />
      </FormProvider>
    </>
  );
};
export default StepOne;
