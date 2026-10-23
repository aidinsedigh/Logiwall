import { Controller, useFormContext } from 'react-hook-form';
import { InputAdornment, TextField, TextFieldProps } from '@mui/material';
import { FC } from 'react';
//-----------------------------------------------------------------------
const convertPersianToEnglish = (value: string) => {
  const persianToEnglishMap: any = {
    '۰': '0',
    '۱': '1',
    '۲': '2',
    '۳': '3',
    '۴': '4',
    '۵': '5',
    '۶': '6',
    '۷': '7',
    '۸': '8',
    '۹': '9',
  };
  return value.replace(/[۰-۹]/g, (d) => persianToEnglishMap[d]);
};
//---------------------------------------------------------------------------------------------------
interface IlgInput extends Omit<TextFieldProps, 'name'> {
  name: string;
  helperText?: string;
  type?: string;
  endAdornment?: string;
}
//---------------------------------------------------------------------------------------

const NumberField: FC<IlgInput> = ({ name, endAdornment, ...other }) => {
  const { control } = useFormContext();

  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState: { error } }) => (
        <TextField
          {...field}
          fullWidth
          sx={{ borderRadius: '20px' }}
          autoComplete="off"
          onChange={(event: any) => {
            const inputValue = event.target.value;
            const convertedValue = convertPersianToEnglish(inputValue);
            field.onChange(convertedValue);
          }}
          value={field.value || ''}
          error={!!error}
          inputProps={{
            style: { fontSize: 16 },
          }}
          helperText={error ? error.message : ''}
          slotProps={{
            input: {
              endAdornment: <InputAdornment position="end">{endAdornment ?? ''}</InputAdornment>,
              inputMode: 'tel', // Ensures mobile browsers display the number keypad
              style: { fontSize: 16 },
            },
          }}
          {...other}
        />
      )}
    />
  );
};
export default NumberField;
