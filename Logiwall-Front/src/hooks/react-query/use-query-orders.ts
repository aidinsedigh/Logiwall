import { useQuery } from '@tanstack/react-query';
import useApiEndpoints from '../use-api-endpoints';
import { axiosInternal } from '../../lib/axios';

const useOrders = () => {
  const endpoints = useApiEndpoints();
  const { data, isFetching } = useQuery({
    queryKey: ['orders-list'],
    queryFn: () => axiosInternal.get(endpoints.GET_ORDERS.GET_ORDERS_LIST),
  });
  return { data, isFetching };
};
export default useOrders;
