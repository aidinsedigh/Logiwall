import { Box, Typography } from '@mui/material';
import StepTitle from '../../../components/ui/title';
import useOtpLogin from '../api/otp';
import FormProvider from '../../../components/ui/form';
import LgButton from '../../../components/ui/button';
import LgIcon from '../../../components/ui/icon';
import PhoneNumberField from '../../../components/ui/phone-number-input';
import { useWindowSize } from 'usehooks-ts';
import { useNavigate } from 'react-router';
import useQueryParam from '../../../hooks/use-query-param';
import { useLocation } from 'react-router-dom';

//-------------------------------------------------------------------------

const OtpSendCode = () => {
  const { width } = useWindowSize();
  const { sendOtpMethods, HandleSendOtpCode } = useOtpLogin();
  const navigate = useNavigate();
  return (
    <>
      <StepTitle title="شماره همراه خود را وارد کنید" />
      <FormProvider methods={sendOtpMethods} onSubmit={HandleSendOtpCode}>
        <PhoneNumberField name="phoneNumber" />
        <Box mt={5} className="flex justify-center">
          <Box
            className="flex text-customColors-primaryOne cursor-pointer"
            onClick={() => {
              navigate('/auth/login', { replace: true });
            }}
          >
            <LgIcon
              width={20}
              className="cursor-pointer mt-2"
              icon="solar:alt-arrow-right-line-duotone"
            />
            <Typography mt={1} fontSize={12}>
              بازگشت
            </Typography>
          </Box>
          <Box ml={width > 900 ? 14 : 10}>
            <LgButton type="submit">ارسال رمز یک بار مصرف</LgButton>
          </Box>
        </Box>
      </FormProvider>
    </>
  );
};
export default OtpSendCode;
