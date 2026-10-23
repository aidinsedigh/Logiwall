import { Box, InputAdornment, Stack } from '@mui/material';
import { useForm } from 'react-hook-form';
import * as Yup from 'yup';
import { yupResolver } from '@hookform/resolvers/yup';
import { useAppDispatch, useAppSelector } from 'store/store';
import { setActiveStep, setStepFiveData } from 'store/slices/create-order-slice';
import StepTitle from 'components/ui/title';
import FormProvider from 'components/ui/form';
import LgInput from 'components/ui/input';
import StepsButtons from 'components/ui/steps-buttons';
import { isValidPhoneNumber } from 'react-phone-number-input';
import PhoneNumberField from 'components/ui/phone-number-input';
import * as yup from 'yup';
import NumberField from 'components/ui/number-field';
import { NumberFieldSchema } from 'lib/yup/yupSchemas';

//--------------------------------------------------------------------------------------------------------------------------
const schema: any = Yup.object().shape({
  senderName: Yup.string()
    .matches(/^[\u0600-\u06FF\s]+$/, 'نام را بدرستی وارد کنید')
    .required('نام فرستنده را وارد کنید'),
  senderLastName: Yup.string()
    .matches(/^[\u0600-\u06FF\s]+$/, 'نام خانوادگی را بدرستی وارد کنید')
    .required('نام خانوادگی فرستنده را وارد کنید'),
  senderPhoneNumber: Yup.string()
    .test('isValidPhoneNumber', 'لطفا شماره تلفن معتبر وارد کنید', (value) => {
      return isValidPhoneNumber(value || '');
    })
    .required('شماره تلفن همراه خود را وارد کنید'),
  originAddress: Yup.string()
    .min(10, 'ادرس باید حداقل 10 کاراکتر داشته باشد')
    .required('ادرس  را وارد کنید'),
  originalPlaque: NumberFieldSchema(
    'پلاک',
    1,
    199,
    'پلاک را بدرستی وارد نمایید',
    'پلاک را بدرستی وارد نمایید'
  ),
  originalUnit: NumberFieldSchema(
    'واحد',
    1,
    199,
    'واحد را بدرستی وارد نمایید',
    'واحد را بدرستی وارد نمایید'
  ),
  originPostalCode: yup
    .string()
    .matches(/^\d*$/, 'کد پستی باید 10 رقمی باشد') // Ensures only digits are allowed
    .test('is-correct-length', 'کد پستی باید 10 رقمی باشد', (value) => {
      if (value && value.length > 0) {
        return value.length === 10; // Only check length if value is not empty
      }
      return true; // Allow empty input
    })
    .notRequired(), // Makes the field optional
});
//--------------------------------------------------------------------------------------------------------------------------

const StepFive = () => {
  const selector: any = useAppSelector((state) => state.CreateOrderSlice);
  const dispatch = useAppDispatch();

  const methods = useForm({
    resolver: yupResolver(schema),
    defaultValues: {
      senderName: selector.stepFiveData.senderName ?? '',
      senderLastName: selector.stepFiveData.senderLastName ?? '',
      senderPhoneNumber: selector.stepFiveData.senderPhoneNumber ?? '',
      originAddress: selector.stepFiveData.originAddress ?? '',
      originalPlaque: selector.stepFiveData.originalPlaque ?? '',
      originalUnit: selector.stepFiveData.originalUnit ?? '',
      originPostalCode: selector.stepFiveData.originPostalCode ?? '',
    },
  });
  const { handleSubmit } = methods;

  const HandleSubmit = handleSubmit((payloads) => {
    dispatch(
      setStepFiveData({
        ...payloads,
        originalPlaque: String(payloads.originalPlaque),
        originalUnit: String(payloads.originalUnit),
        originPostalCode: String(payloads.originPostalCode),
      })
    );
    dispatch(setActiveStep(selector.activeStep + 1));
  });

  return (
    <Box className="flex flex-col min-h-screen">
      <Box my={0}>
        <StepTitle
          icon={'lets-icons:folder-send-duotone'}
          className="text-customColors-primaryOne mr-5"
          title={'اطلاعات فرستنده خود را وارد کنید'}
        />
      </Box>
      <FormProvider methods={methods} onSubmit={HandleSubmit}>
        <Stack px={3} spacing={4} mb={5}>
          <LgInput name="senderName" label="نام فرستنده" placeholder="نام فرستنده" />
          <LgInput
            name="senderLastName"
            label="نام خانوادگی فرستنده"
            placeholder="نام خانوادگی فرستنده"
          />
          <PhoneNumberField name="senderPhoneNumber" fullWidth />
          <LgInput name="originAddress" label="جزییات ادرس" placeholder="جزییات ادرس" />
          <NumberField name="originalPlaque" label="پلاک" />
          <NumberField name="originalUnit" label="واحد" />
          <NumberField name="originPostalCode" label="کدپستی" />
        </Stack>
        <StepsButtons />
      </FormProvider>
    </Box>
  );
};
export default StepFive;
