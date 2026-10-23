import { useMutation, UseMutationResult } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import axios from 'axios';
import { useNavigate } from 'react-router';
import { setTrackData } from 'store/slices/tracking-slice';
import { useAppDispatch } from 'store/store';

const useTracking = () => {
  const methods = useForm({
    defaultValues: {
      Code: '',
    },
  });
  const { handleSubmit } = methods;
  const navigate = useNavigate();
  const dispatch = useAppDispatch();
  const { mutateAsync, isPending } = useMutation({
    mutationKey: ['tracking'],
    mutationFn: (payload) =>
      axios.post(
        'http://jet.tipax.ir:200/odata/Tipax/DspContractTrackings/Tipax.TrackingParcelWithDetail',
        payload,
        {
          headers: {
            Authorization: `Bearer eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Ik1ZR0FtX2hHeWRvYWhrSGtlcjRlaTRiTXRSUSIsImtpZCI6Ik1ZR0FtX2hHeWRvYWhrSGtlcjRlaTRiTXRSUSJ9.eyJpc3MiOiJUaXBheCIsImF1ZCI6IlRpcGF4L3Jlc291cmNlcyIsImV4cCI6MTc1MzUxNjY4MCwibmJmIjoxNzE0NjM2NjgwLCJjbGllbnRfaWQiOiJUaXBheC1BUElVc2VyWG1vbnRoIiwic2NvcGUiOlsib3BlbmlkIiwicHJvZmlsZSIsInVzZXJfaW5mbyJdLCJzdWIiOiJiZmQyYzZhMC04YmE1LTRjYTEtOWRjZC1iOTVjMmRkMmY0NTQiLCJhdXRoX3RpbWUiOjE3MTQ2MzY2ODAsImlkcCI6Imlkc3J2IiwicHJpbWFyeV9zaWQiOiJiZmQyYzZhMC04YmE1LTRjYTEtOWRjZC1iOTVjMmRkMmY0NTQiLCJ1cG4iOiJiZmQyYzZhMC04YmE1LTRjYTEtOWRjZC1iOTVjMmRkMmY0NTQiLCJuYW1lIjoiYmZkMmM2YTAtOGJhNS00Y2ExLTlkY2QtYjk1YzJkZDJmNDU0IiwiZ2l2ZW5fbmFtZSI6ImJmZDJjNmEwLThiYTUtNGNhMS05ZGNkLWI5NWMyZGQyZjQ1NCIsImp0aSI6IjM5YjllYWFjODZlZjJjZDhmMDBiNTk4ODY2Nzk3YjA1IiwiYW1yIjpbImN1c3RvbSJdfQ.b6_cygkdbAFeI-nSYW4ZcHGydruKMWELQhmWT1xrUI9WGV2OUn_7btQiKpTXJBrriHaD_VHe5oLEETuG5g6rHMml0IkhgAhdLDmFPshXJAsgO-eSekkbYyImGKR1tzpSBPBIqiedrR87hZ7NPWB0GVXg5VDjtlSbgXvjLWcSRCkXM52TbXh819ZSd8aMvGUfwiNVJG-WV12nCqdZ64bnNNMagiOrOxyNibDToTd2mQEjZWM-MEiy2qJ2U_AdbgFSclq0wajHVkuRNzNwIX0bFKYSszWEZDUXOwEC_tZXLFyMOIya0WWd4_hczqy0Qe48roaKXoYVXILgfG6Ww5Uw8A`,
          },
        }
      ),
  }) as UseMutationResult<any>;

  const OnSubmit = handleSubmit(async (payload) => {
    try {
      const response = await mutateAsync(payload);
      dispatch(setTrackData(response?.data?.Data[0]));
      navigate('/app/parcel-tracking/result', { replace: true });
    } catch (e) {
      console.log(e);
    }
  });
  return { OnSubmit, methods, isPending };
};
export default useTracking;
