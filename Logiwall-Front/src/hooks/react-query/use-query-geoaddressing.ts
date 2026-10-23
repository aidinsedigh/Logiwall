import { useMutation } from '@tanstack/react-query';
import axios from 'axios';

const useQueryGeoAddressing = () => {
  return useMutation({
    mutationKey: ['mapir'],
    mutationFn: (data: any) =>
      axios.get(
        `/odata/Tipax/CmnCities/Tipax.ReverseLocationToAddress(lat='${data[0]}',lng='${data[1]}')`,
        {
          baseURL: 'https://jetgw.tipax.ir',
          headers: { Authorization: `Bearer ${process.env.REACT_APP_MAP_TIPAX_TOKEN}` },
        }
      ),
  });
};
export default useQueryGeoAddressing;
