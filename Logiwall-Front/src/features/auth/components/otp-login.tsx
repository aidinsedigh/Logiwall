import * as React from 'react';
import useQueryParam from '../../../hooks/use-query-param';
import OtpConfirmCode from './otp-code-confirm';
import OtpSendCode from './otp-send-code';
import { useEffect } from 'react';

//--------------------------------------------------------------------------------------

const OtpLogin = () => {
  const { GetQuery } = useQueryParam();
  return <>{GetQuery.phoneNumber ? <OtpConfirmCode /> : <OtpSendCode />}</>;
};
export default OtpLogin;
