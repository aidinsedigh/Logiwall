import { Box } from '@mui/material';
import StepTitle from '../../../components/ui/title';
import FormProvider from '../../../components/ui/form';
import LgInput from '../../../components/ui/input';
import LgButton from '../../../components/ui/button';
import useRegister from '../api/register';
import LgRadio from '../../../components/ui/radio';
import NumberField from '../../../components/ui/number-field';

//----------------------------------------------------------------------------------------------

const Register = () => {
  const { HandleSubmitRegister, methods, RegisterPending } = useRegister();
  return (
    <>
      <StepTitle title="لطفا اطلاعات خود را تکمیل کنید" />
      <FormProvider onSubmit={HandleSubmitRegister} methods={methods}>
        <Box className="space-y-5 md:px-32">
          <LgInput name="name" label="نام" />
          <LgInput name="lastName" label="نام خانوادگی" />
          <NumberField name="nationalCode" label="کد ملی " />
          <LgInput name="email" label="ایمیل ( اختیاری )" />
          <LgInput type="password" name="password" label="رمز عبور" />
          <LgInput type="password" name="confirmPassword" label="تکرار رمز عبور" />
          <Box>
            <LgButton isLoading={RegisterPending} type="submit" className="w-full md:w-96">
              تایید
            </LgButton>
          </Box>
        </Box>
      </FormProvider>
    </>
  );
};
export default Register;
