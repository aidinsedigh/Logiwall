import { useForm } from 'react-hook-form';
import { yupResolver } from '@hookform/resolvers/yup';
import useApiEndpoints from 'hooks/use-api-endpoints';
import { useMutation, UseMutationResult } from '@tanstack/react-query';
import { axiosInternal } from 'lib/axios';
import { toast } from 'sonner';
import * as yup from 'yup';
import * as Yup from 'yup';
import { IchangePasswordResponse } from '../../../types/profile';

// -------------------------------------------------------------------------

const schema = yup.object().shape({
  oldPassword: Yup.string().required('لطفا رمز عبور فعلی خود را وارد کنید'),
  newPassword: yup.string().required('لطفا رمز عبور جدید خود را وارد کنید'),
});

// -------------------------------------------------------------------------

const useChangePassword = () => {
  const methods = useForm({
    resolver: yupResolver(schema),
    defaultValues: {
      oldPassword: '',
      newPassword: '',
    },
  });

  const { handleSubmit } = methods;

  const endpoints = useApiEndpoints();

  const { mutateAsync, isPending } = useMutation({
    mutationKey: ['change-password'],
    mutationFn: (payload) =>
      axiosInternal.post(endpoints.PROFILE.POST_CHANGE_PASSWORD, payload).then((res) => res.data),
  }) as UseMutationResult<IchangePasswordResponse>;

  const HandleChangePassword = handleSubmit((payloads) => {
    try {
      const res = mutateAsync(payloads);
      console.log(res);
      toast.success('رمز عبور با موفقیت تغییر داده شد');
    } catch (error) {
      console.log(error);
    }
  });

  return { HandleChangePassword, isPending, methods };
};
export default useChangePassword;
