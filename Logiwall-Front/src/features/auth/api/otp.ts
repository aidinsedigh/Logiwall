import { useForm } from 'react-hook-form';
import { useMutation, UseMutationResult, useQuery, UseQueryResult } from '@tanstack/react-query';
import { useNavigate } from 'react-router';
import useQueryParam from '../../../hooks/use-query-param';
import { useAppDispatch } from '../../../store/store';
import { axiosAuth } from '../../../lib/axios';
import { setToken } from '../../../store/slices/auth-slice';
import useApiEndpoints from '../../../hooks/use-api-endpoints';
import { yupResolver } from '@hookform/resolvers/yup';
import * as Yup from 'yup';
import { isValidPhoneNumber } from 'react-phone-number-input';
import useAuthLogin from '../../../hooks/use-login';
import { IconfirmedCodePayload, IconfirmOtpResponse, IsendOtpResponse } from '../../../types/auth';

//------------------------------------------------------------------------
const sendOtpSchema = Yup.object().shape({
  phoneNumber: Yup.string()
    .test('isValidPhoneNumber', 'لطفا شماره تلفن معتبر وارد کنید', (value) => {
      return isValidPhoneNumber(value || '');
    })
    .required('شماره تلفن همراه خود را وارد کنید'),
});

const confirmOtpSchema = Yup.object().shape({
  code: Yup.string().required('لطفا کد ارسالی به شماره همراه خود را وارد کنید'),
  phoneNumber: Yup.string(),
  channel: Yup.string(),
  loginProvider: Yup.number(),
});
//-------------------------------------------------------------------------------------
const useOtpLogin = () => {
  const { setQuery, GetQuery }: any = useQueryParam();
  const dispatch = useAppDispatch();
  const navigate = useNavigate();
  const endpoints = useApiEndpoints();
  const { Login } = useAuthLogin();
  const sendOtpMethods = useForm({ resolver: yupResolver(sendOtpSchema) });
  const confirmOtpMethods = useForm({
    resolver: yupResolver(confirmOtpSchema),
    defaultValues: {
      phoneNumber: GetQuery.phoneNumber,
      channel: GetQuery?.channel ?? 'pwa',
      code: '',
      loginProvider: GetQuery?.channel ? 21 : 20,
    },
  });
  const { control: sendOtpControl, handleSubmit: sendOtpHandleSubmit } = sendOtpMethods;
  const { control: confirmOtpControl, handleSubmit: confirmOtpHandleSubmit } = confirmOtpMethods;

  // send otp code mutate
  const { isPending: sendOtpPending, refetch: ResendOtpCode }: any = useQuery({
    queryKey: ['send-otp-code'],
    queryFn: async () => {
      return axiosAuth
        .post(endpoints.AUTH.POST_SEND_OTP_CODE, {
          phoneNumber: GetQuery?.phoneNumber,
          channel: GetQuery?.channel ?? 'pwa',
          loginProvider: GetQuery?.channel ? 21 : 20,
        })
        .then((res) => res);
    },
    enabled: !!GetQuery?.phoneNumber,
  }) as UseQueryResult<IsendOtpResponse>;

  const HandleSendOtpCode = sendOtpHandleSubmit((payload) => {
    setQuery({ phoneNumber: payload.phoneNumber.replace('+98', '0') });
  });

  //confirm otp code mutate
  const { mutateAsync: ConfirmOtpCode, isPending: confirmOtpPending } = useMutation({
    mutationKey: ['confirmationOtpCode'],
    mutationFn: (payloads) => {
      return axiosAuth
        .post(endpoints.AUTH.POST_CONFIRM_OTP_CODE, payloads)
        .then((res: any) => res.data.data);
    },
  }) as UseMutationResult<IconfirmOtpResponse>;

  const HandleConfirmOtpCode = confirmOtpHandleSubmit(async (payloads: IconfirmedCodePayload) => {
    console.log(payloads);
    try {
      const response = await ConfirmOtpCode(payloads);
      if (!response.fullName) {
        dispatch(setToken({ token: response.accessToken, expTime: response.expiresIn }));
        navigate('/auth/register', { replace: true });
      } else {
        Login(response.accessToken, response.expiresIn);
      }
    } catch (error) {
      console.log(error);
    }
  });

  return {
    confirmOtpMethods,
    confirmOtpControl,
    sendOtpMethods,
    sendOtpControl,
    sendOtpPending,
    HandleConfirmOtpCode,
    confirmOtpPending,
    HandleSendOtpCode,
    ResendOtpCode,
  };
};
export default useOtpLogin;
