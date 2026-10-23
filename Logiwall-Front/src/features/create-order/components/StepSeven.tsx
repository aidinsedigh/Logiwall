import { Box, Stack } from '@mui/material';
import StepTitle from 'components/ui/title';
import FormProvider from 'components/ui/form';
import LgInput from 'components/ui/input';
import PhoneNumberField from 'components/ui/phone-number-input';
import StepsButtons from 'components/ui/steps-buttons';
import NumberField from 'components/ui/number-field';
import useCreateOrder from 'features/create-order/api/create-order';
import LgAnimateLoading from 'components/ui/loading/animate-loading';

// -----------------------------------------------------------------------------------------------

const StepSeven = () => {
  const { HandleSubmit, methods, CreateOrderPending } = useCreateOrder();

  return (
    <>
      {CreateOrderPending ? (
        <LgAnimateLoading />
      ) : (
        <Box className="flex flex-col min-h-screen">
          <Box my={2}>
            <StepTitle
              isMapTitle
              icon={'solar:send-square-bold-duotone'}
              className="text-customColors-primaryOne mr-5"
              title={'اطلاعات گیرنده خود را وارد کنید'}
            />
          </Box>
          <FormProvider methods={methods} onSubmit={HandleSubmit}>
            <Stack px={3} spacing={4} mb={10}>
              <LgInput name="receiverName" label="نام گیرنده" placeholder="نام گیرنده" />
              <LgInput
                name="receiverLastName"
                label="نام خانوادگی فرستنده"
                placeholder="نام خانوادگی فرستنده"
              />
              <PhoneNumberField name="receiverPhoneNumber" fullWidth />
              <LgInput name="destinationAddress" label="جزییات ادرس" placeholder="جزییات ادرس" />
              <NumberField name="destinationPlaque" label="پلاک" />
              <NumberField name="destinationUnit" label="واحد" />
              <NumberField name="destinationPostalCode" label="کدپستی" />
            </Stack>
            <StepsButtons />
          </FormProvider>
        </Box>
      )}
      ;
    </>
  );
};
export default StepSeven;
