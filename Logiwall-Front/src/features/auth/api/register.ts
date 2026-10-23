import { useForm } from 'react-hook-form';
import { useAppSelector } from '../../../store/store';
import { axiosAuth } from '../../../lib/axios';
import useAuthLogin from '../../../hooks/use-login';
import { useMutation, UseMutationResult } from '@tanstack/react-query';
import useApiEndpoints from '../../../hooks/use-api-endpoints';
import * as Yup from 'yup';
import { yupResolver } from '@hookform/resolvers/yup';
import * as yup from 'yup';
import { IregisterPayload } from '../../../types/auth';

//------------------------------------------------------------------------------
const schema = Yup.object().shape({
  name: Yup.string()
    .matches(/^[\u0600-\u06FF\s]+$/, 'نام خود را بدرستی وارد کنید')
    .required('لطفا نام خود را وارد کنید'),
  lastName: Yup.string()
    .matches(/^[\u0600-\u06FF\s]+$/, 'نام خانوادگی خود را بدرستی وارد کنید')
    .required('لطفا نام خانوادگی خود را وارد کنید'),
  password: Yup.string().required('لطفا رمز عبور خود را وارد کنید'),
  confirmPassword: Yup.string()
    .oneOf([Yup.ref('password')], 'رمز عبور و نکرار رمز عبور یکسان نیست')
    .required('لطفا تکرار رمز عبور را وارد کنید'),
  nationalCode: yup
    .string()
    .matches(/^\d*$/, 'کد ملی باید 10 رقمی باشد') // Ensures only digits are allowed
    .test('is-correct-length', 'کد ملی باید 10 رقمی باشد', (value) => {
      if (value && value.length > 0) {
        return value.length === 10; // Only check length if value is not empty
      }
      return true; // Allow empty input
    })
    .required(),
});
//-------------------------------------------------------------------------------

const useRegister = () => {
  const endpoints = useApiEndpoints();

  const methods = useForm({
    resolver: yupResolver(schema),
    defaultValues: {
      confirmPassword: '',
      name: '',
      lastName: '',
      password: '',
      nationalCode: '',
    },
  });

  const { Login } = useAuthLogin();
  const selector: any = useAppSelector((state) => state.AuthSlice);
  const { handleSubmit, control } = methods;
  const { mutateAsync: Register, isPending: RegisterPending } = useMutation({
    mutationKey: ['register'],
    mutationFn: (params) => {
      return axiosAuth
        .post(endpoints.AUTH.POST_REGISTER, params, {
          headers: {
            Authorization: 'Bearer ' + selector.token,
            'Content-Type': 'application/json',
          },
        })
        .then((res) => console.log(res));
    },
  }) as UseMutationResult<any>;
  const HandleSubmitRegister = handleSubmit(async (payload: IregisterPayload) => {
    try {
      await Register(payload);
      Login(selector.token, selector.expTime);
    } catch (error) {
      console.log(error);
    }
  });
  return { methods, control, HandleSubmitRegister, RegisterPending };
};
export default useRegister;
