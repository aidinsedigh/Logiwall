import { TextField, TextFieldProps } from '@mui/material';
import { Controller, useFormContext } from 'react-hook-form';
import { FC } from 'react';

//---------------------------------------------------------------------------------------
interface IlgInput extends Omit<TextFieldProps, 'name'> {
  name: string;
  helperText?: string;
  type?: string;
}
//---------------------------------------------------------------------------------------

const LgInput: FC<IlgInput> = ({ name, helperText, type, ...other }) => {
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
          type={type}
          value={type === 'number' && field.value === 0 ? '' : field.value}
          onChange={(event) => {
            if (type === 'number') {
              field.onChange(Number(event.target.value));
            } else {
              field.onChange(event.target.value);
            }
          }}
          error={!!error}
          inputProps={{
            style: { fontSize: 16 },
          }}
          helperText={error ? error?.message : helperText}
          {...other}
        />
      )}
    />
  );
};
export default LgInput;
