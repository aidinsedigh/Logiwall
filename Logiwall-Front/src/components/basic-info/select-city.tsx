import { Controller } from 'react-hook-form';
import { Autocomplete, TextField, TextFieldProps } from '@mui/material';
import useQueryDestinationCities from '../../hooks/react-query/use-query-destination-cities';
import useQueryOriginCities from '../../hooks/react-query/use-query-origin-cities';
import { FC } from 'react';
import { useAppDispatch, useAppSelector } from '../../store/store';
import { setCitiesName, setStepOneData } from '../../store/slices/create-order-slice';

// --------------------------------------------------------------------

interface IselectCity {
  control: any;
  name: string;
  isOrigin: boolean;
  textFieldProps?: TextFieldProps;
}

// --------------------------------------------------------------------
const SelectCity: FC<IselectCity> = ({ control, name, isOrigin, textFieldProps }) => {
  const { data: OriginCities } = useQueryOriginCities();
  const { data: DestinationCities } = useQueryDestinationCities();

  const dispatch = useAppDispatch();
  const selector = useAppSelector((state) => state?.CreateOrderSlice?.cities);

  return (
    <Controller
      name={name}
      control={control}
      render={({ field: { onChange, value } }) => (
        <Autocomplete
          options={isOrigin ? OriginCities : DestinationCities}
          getOptionLabel={(option) => option.title || ''}
          isOptionEqualToValue={(option, value) => option.id === value?.id}
          onChange={(event, newValue) => {
            dispatch(
              setCitiesName(
                isOrigin
                  ? { ...selector, source: newValue?.title ?? 'تهران' }
                  : { ...selector, destination: newValue?.title }
              )
            );
            onChange(newValue?.id);
          }}
          renderInput={(params) => (
            <TextField
              {...params}
              variant="outlined"
              label={isOrigin ? 'مبدا' : 'مقصد'}
              fullWidth
              {...textFieldProps}
            />
          )}
          value={
            (isOrigin ? OriginCities : DestinationCities)?.find(
              (option: any) => option.id === value
            ) || null
          }
        />
      )}
    />
  );
};
export default SelectCity;
